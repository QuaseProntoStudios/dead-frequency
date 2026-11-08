// Teste rápido contra `npm run dev` (ou passe a URL: node test/smoke.mjs https://seu-worker.workers.dev)
import WebSocket from "ws";

const base = (process.argv[2] ?? "http://127.0.0.1:8787").replace(/\/$/, "");
const wsBase = base.replace(/^http/, "ws");
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const assert = (cond, label) => {
  if (!cond) throw new Error("FALHOU: " + label);
  console.log("ok -", label);
};

function connect(code, name, prof = "entregador") {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(`${wsBase}/api/rooms/${code}/ws?name=${encodeURIComponent(name)}`);
    const inbox = [];
    ws.on("message", (data) => inbox.push(JSON.parse(data.toString())));
    ws.on("open", () => {
      ws.send(JSON.stringify({ t: "hello", version: 4, prof }));
      resolve({ ws, inbox, send: (m) => ws.send(JSON.stringify(m)), find: (t) => inbox.find((m) => m.t === t), all: (t) => inbox.filter((m) => m.t === t) });
    });
    ws.on("error", reject);
  });
}

const health = await (await fetch(base + "/")).json();
assert(health.ok, "health");

const { code } = await (await fetch(base + "/api/rooms", { method: "POST" })).json();
assert(/^[A-Z2-9]{5}$/.test(code), "sala criada " + code);

const info = await (await fetch(`${base}/api/rooms/${code}`)).json();
assert(info.players === 0, "info da sala");
assert((await fetch(`${base}/api/rooms/ZZZZZ`)).status === 404, "sala inexistente = 404");

const a = await connect(code, "Teuzin");
await sleep(300);
const welcomeA = a.find("welcome");
assert(welcomeA && welcomeA.code === code && welcomeA.jobs.length >= 3, "welcome A com pedidos");
assert(welcomeA.level === "city", "sala sem fase = cidade");

// Sala do rancho (fase 2).
const { code: farmCode } = await (await fetch(base + "/api/rooms", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ level: "farm" }) })).json();
const farmer = await connect(farmCode, "Caipira");
await sleep(300);
assert(farmer.find("welcome")?.level === "farm", "sala criada com a fase rancho");
farmer.ws.close();
const { code: badCode } = await (await fetch(base + "/api/rooms", { method: "POST", body: JSON.stringify({ level: "lua" }) })).json();
const lost = await connect(badCode, "Perdido");
await sleep(300);
assert(lost.find("welcome")?.level === "city", "fase desconhecida vira cidade");
lost.ws.close();

const b = await connect(code, "Amigo<script>");
await sleep(300);
const welcomeB = b.find("welcome");
assert(welcomeB.players.length === 1 && welcomeB.seed === welcomeA.seed, "B vê A e mesma seed");
assert(welcomeB.name === "Amigoscript", "nome sanitizado");
assert(a.find("join")?.player.id === welcomeB.you, "A recebeu join de B");

const job = welcomeA.jobs[0];
a.send({ t: "claim", id: job.id });
await sleep(200);
assert(b.all("owner").some((m) => m.id === job.id && m.owner === welcomeA.you), "posse da caixa passou para A");

a.send({ t: "tick", p: [1, 0, 2], ry: 0.5, hp: 0, st: 0, held: job.id, objs: [{ id: job.id, p: [1, 1, 1], q: [0, 0, 0, 1], integ: 0.5 }, { id: "van", p: [9, 9, 9], q: [0, 0, 0, 1] }] });
await sleep(200);
const tick = b.find("tick");
assert(tick && tick.objs.length === 1 && tick.objs[0].id === job.id, "tick repassado e van (sem posse) filtrada");

b.send({ t: "claim", id: job.id });
await sleep(200);
assert(b.find("claimFail")?.id === job.id, "B não toma caixa que A está segurando");

b.send({ t: "deliver", cargoId: job.id, integ: 1 });
a.send({ t: "deliver", cargoId: job.id, integ: 0.5 });
await sleep(300);
const delivered = b.all("delivered");
assert(delivered.length === 1 && delivered[0].by === welcomeA.you, "só o dono entrega");
assert(delivered[0].payment === Math.round([150, 300, 120, 180, 220, 90, 160, 140, 260, 280, 200, 110, 170][job.type] * 0.5), "pagamento proporcional à integridade");

for (const j of welcomeA.jobs.slice(1)) {
  a.send({ t: "claim", id: j.id });
  await sleep(50);
  a.send({ t: "deliver", cargoId: j.id, integ: 1 });
}
await sleep(500);
assert(a.find("shiftDone"), "turno concluído");
await sleep(5000);
const shift = a.find("shift");
assert(shift && shift.shift === 2 && shift.jobs.every((j) => j.status === "pending"), "novo turno via alarm");

b.send({ t: "claim", id: "van" });
await sleep(150);
b.send({ t: "tick", p: [0, 0, 0], ry: 0, hp: 0, st: 1, held: null, objs: [] });
await sleep(150);
a.send({ t: "claim", id: "van" });
await sleep(200);
assert(a.find("claimFail")?.id === "van", "não dá para tomar a van de quem está dirigindo");

// ---------------- loja e game over ----------------
const moneyBefore = a.all("delivered").at(-1)?.money ?? 0;
a.send({ t: "buy", item: "tape", req: 7 });
await sleep(200);
const bought = b.all("money").find((m) => m.req === 7);
assert(bought && bought.money === moneyBefore - 50 && bought.item === "tape", "compra desconta do dinheiro do grupo");
a.send({ t: "buy", item: "radar", req: 8 });
a.send({ t: "buy", item: "nave-espacial", req: 9 });
await sleep(200);
const fails = a.all("buyFail").map((m) => m.req);
assert(fails.includes(9) && (moneyBefore - 50 < 400 ? fails.includes(8) : true), "compra sem dinheiro / item inválido recusada");
a.send({ t: "tick", p: [0, 0, 0], ry: 0, hp: 0, st: 0, held: null, eq: "radar,gloves<x>", objs: [] });
await sleep(150);
assert(b.all("tick").at(-1)?.eq === "radar,glovesx", "equipamento repassado e filtrado");
const shift2 = a.all("shift").at(-1);
for (const j of shift2.jobs) {
  a.send({ t: "claim", id: j.id });
  await sleep(40);
  a.send({ t: "lost", cargoId: j.id });
}
await sleep(400);
const over = b.find("gameOver");
assert(over && over.money === 0, "todas as caixas perdidas = game over e dinheiro zerado");
await sleep(8800);
const restart = b.all("shift").at(-1);
assert(restart.shift === 1 && restart.money === 0, "sala recomeça no turno 1 depois do game over");

// ---------------- profissões (v0.9) ----------------
{
  const { code: pc } = await (await fetch(base + "/api/rooms", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ autoProfession: true }) })).json();
  const p1 = await connect(pc, "Um", "gari");
  await sleep(250);
  const p2 = await connect(pc, "Dois", "gari");
  await sleep(250);
  const w1 = p1.find("welcome"), w2 = p2.find("welcome");
  assert(w1.autoProfession === true && w1.prof === "entregador" && w2.prof === "taxista", "sala automática equilibra as profissões");
  assert(w2.players[0].prof === "entregador", "welcome mostra a profissão dos outros");
  p2.send({ t: "prof", prof: "gari" });
  await sleep(200);
  assert(p2.all("prof").at(-1)?.prof === "taxista", "sala automática não deixa trocar");
  p1.send({ t: "earn", job: "taxista", amount: 300 });
  await sleep(150);
  assert(p1.find("earnFail"), "não recebe por profissão que não é a sua");
  p2.send({ t: "earn", job: "taxista", amount: 99999, what: "corrida" });
  await sleep(150);
  const e1 = p1.all("earned").at(-1);
  assert(e1 && e1.payment === 400 && e1.by === w2.you, "corrida limitada ao máximo e avisada para a sala");
  p2.send({ t: "earn", job: "taxista", amount: 100 });
  await sleep(150);
  assert(p2.all("earnFail").length === 1, "intervalo mínimo entre pagamentos");
  p1.ws.close(); p2.ws.close();

  const { code: fc } = await (await fetch(base + "/api/rooms", { method: "POST" })).json();
  const g = await connect(fc, "Gari", "gari");
  const r = await connect(fc, "Repo", "locutor");
  await sleep(300);
  assert(g.find("welcome").prof === "gari" && r.find("welcome").prof === "locutor", "sala livre respeita a escolha");
  assert(g.find("welcome").jobs.length === 3, "turno conta só os entregadores");
  g.send({ t: "earn", job: "gari", units: 9 });
  await sleep(150);
  assert(g.all("earned").at(-1)?.payment === 75, "gari: no máximo 3 sacos por pagamento");
  r.send({ t: "prof", prof: "repositor" });
  await sleep(150);
  assert(g.all("prof").some((m) => m.prof === "repositor" && m.id === r.find("welcome").you), "troca de profissão avisada");
  g.ws.close(); r.ws.close();
}

// ---------------- admin ----------------
const token = process.env.ADMIN_TOKEN ?? "teste-local";
const admin = (path, init = {}) => fetch(base + "/api/admin" + path, { ...init, headers: { Authorization: "Bearer " + token, "Content-Type": "application/json" } });
assert((await fetch(base + "/admin")).headers.get("content-type").includes("text/html"), "página /admin");
assert((await fetch(base + "/api/admin/status")).status === 401, "admin sem token = 401");
const status = await (await admin("/status")).json();
const listed = status.rooms.find((r) => r.code === code);
assert(listed && listed.players === 2 && status.stats.deliveries >= welcomeA.jobs.length, "status lista a sala, jogadores e entregas");
const detail = await (await admin("/rooms/" + code)).json();
assert(detail.players.length === 2 && detail.jobs.length > 0 && detail.vanOwner === welcomeB.you, "detalhes da sala (jogadores, pedidos, dono da van)");
await (await admin("/announce", { method: "POST", body: JSON.stringify({ msg: "teste de aviso" }) })).json();
await sleep(200);
assert(a.all("chat").some((m) => m.name === "SERVIDOR" && m.msg === "teste de aviso"), "aviso chega nas salas");

b.ws.close();
await sleep(400);
assert(a.find("leave")?.id === welcomeB.you, "saída avisada");
assert(a.all("owner").some((m) => m.id === "van" && m.owner === null), "van liberada quando o motorista sai");

const closeRes = await (await admin("/rooms/" + code + "/close", { method: "POST" })).json();
await sleep(300);
assert(closeRes.closed && a.find("kick")?.msg.includes("encerrada"), "admin encerra a sala e manda kick");
assert((await fetch(`${base}/api/rooms/${code}`)).status === 404, "sala encerrada some");
const after = await (await admin("/status")).json();
assert(!after.rooms.some((r) => r.code === code), "sala encerrada sai do registro");

console.log("\nTudo certo ✔");
process.exit(0);
