using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjectManagerWeb.src.Services.Monitoramento.Uso;

internal static class RelatorioUsoTemplate
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Gerar(DadosRelatorio dados)
    {
        var json = JsonSerializer.Serialize(dados, _jsonOptions).Replace("</", "<\\/");
        return Html.Replace("__DADOS__", json);
    }

    private const string Html = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Registro de consumo do notebook</title>
<style>
  :root {
    --fundo: #0f1115;
    --card: #171a21;
    --card-alt: #1d2129;
    --borda: #2a2f3a;
    --texto: #e8eaee;
    --texto-fraco: #98a1b3;
    --primaria: #4f8cff;
    --alerta: #ff6b57;
    --aviso: #ffb347;
  }
  * { box-sizing: border-box; }
  body {
    margin: 0;
    padding: 26px clamp(16px, 4vw, 48px) 48px;
    background: var(--fundo);
    color: var(--texto);
    font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Arial, sans-serif;
    font-size: 14px;
    line-height: 1.4;
  }
  h1 { font-size: 19px; margin: 0 0 16px; font-weight: 600; }
  h2 {
    font-size: 11px;
    margin: 0 0 8px;
    font-weight: 600;
    letter-spacing: .06em;
    text-transform: uppercase;
    color: var(--texto-fraco);
  }
  .topo {
    display: grid;
    grid-template-columns: minmax(240px, 1fr) minmax(240px, 1fr);
    gap: 16px;
    margin-bottom: 14px;
  }
  @media (max-width: 720px) { .topo { grid-template-columns: 1fr; } }
  .bloco {
    background: var(--card);
    border: 1px solid var(--borda);
    border-radius: 12px;
    padding: 14px 16px;
  }
  .maquina { display: grid; grid-template-columns: auto 1fr; gap: 3px 10px; margin: 0; font-size: 12px; }
  .maquina dt { color: var(--texto-fraco); white-space: nowrap; }
  .maquina dd { margin: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .top-mini { width: 100%; border-collapse: collapse; font-size: 12px; }
  .top-mini td { padding: 2px 0; border: 0; }
  .top-mini td.pos { width: 20px; color: var(--texto-fraco); font-variant-numeric: tabular-nums; }
  .top-mini td.nome { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
  .cards { display: grid; grid-template-columns: repeat(auto-fit, minmax(220px, 1fr)); gap: 12px; margin-bottom: 14px; }
  .card { background: var(--card); border: 1px solid var(--borda); border-radius: 12px; padding: 14px 18px; }
  .card .rotulo { color: var(--texto-fraco); font-size: 11px; text-transform: uppercase; letter-spacing: .06em; }
  .card .valor { font-size: 30px; font-weight: 600; margin-top: 4px; font-variant-numeric: tabular-nums; }
  .painel {
    background: var(--card);
    border: 1px solid var(--borda);
    border-radius: 12px;
    padding: 16px 18px;
  }
  .aviso {
    background: rgba(255, 179, 71, .1);
    border: 1px solid rgba(255, 179, 71, .35);
    color: var(--aviso);
    border-radius: 10px;
    padding: 9px 13px;
    font-size: 12px;
    margin-bottom: 12px;
  }
  .tabela-wrap { overflow-x: auto; }
  table.registros { width: 100%; border-collapse: collapse; font-variant-numeric: tabular-nums; }
  table.registros thead th {
    text-align: right;
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: .05em;
    color: var(--texto-fraco);
    font-weight: 600;
    padding: 8px 10px;
    border-bottom: 1px solid var(--borda);
    white-space: nowrap;
  }
  table.registros thead th:first-child, table.registros tbody td:first-child { text-align: left; }
  table.registros tbody td {
    padding: 6px 10px;
    border-bottom: 1px solid rgba(42, 47, 58, .55);
    text-align: right;
    white-space: nowrap;
  }
  table.registros tbody tr.linha { cursor: pointer; }
  table.registros tbody tr.linha:hover, table.registros tbody tr.linha.aberta { background: var(--card-alt); }
  .cpu-alta { color: var(--alerta); font-weight: 600; }
  .cpu-media { color: var(--aviso); }
  tr.detalhe td { text-align: left; padding: 0; border-bottom: 1px solid var(--borda); }
  .detalhe-conteudo {
    padding: 12px 12px 16px;
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
    gap: 18px;
  }
  .detalhe-bloco h3 {
    font-size: 11px;
    text-transform: uppercase;
    letter-spacing: .06em;
    color: var(--texto-fraco);
    margin: 0 0 8px;
  }
  .detalhe-bloco dl { display: grid; grid-template-columns: auto 1fr; gap: 3px 12px; margin: 0; font-size: 13px; }
  .detalhe-bloco dt { color: var(--texto-fraco); }
  .detalhe-bloco dd { margin: 0; text-align: right; font-variant-numeric: tabular-nums; }
  .proc-tabela { width: 100%; font-size: 13px; border-collapse: collapse; }
  .proc-tabela td { padding: 2px 0; border: 0; }
  .proc-tabela td:last-child { text-align: right; }
  .paginacao { display: flex; align-items: center; justify-content: space-between; gap: 12px; margin-top: 12px; flex-wrap: wrap; }
  .paginacao button {
    background: var(--card-alt);
    color: var(--texto);
    border: 1px solid var(--borda);
    border-radius: 8px;
    padding: 6px 14px;
    cursor: pointer;
    font-size: 13px;
  }
  .paginacao button:disabled { opacity: .4; cursor: default; }
  .paginacao .info { color: var(--texto-fraco); font-size: 12px; }
  .vazio { color: var(--texto-fraco); font-size: 12px; }
  footer { color: var(--texto-fraco); font-size: 11px; margin-top: 20px; }
</style>
</head>
<body>
<h1>Registro de consumo do notebook</h1>

<section class="topo">
  <div class="bloco">
    <h2>Máquina</h2>
    <dl class="maquina" id="maquina"></dl>
  </div>
  <div class="bloco">
    <h2>Apps que mais consumiram CPU</h2>
    <table class="top-mini"><tbody id="topProcessos"></tbody></table>
  </div>
</section>

<section class="cards" id="cards"></section>

<section class="painel">
  <div id="aviso"></div>
  <div class="tabela-wrap">
    <table class="registros">
      <thead>
        <tr>
          <th>Data / hora</th>
          <th>CPU</th>
          <th>RAM</th>
          <th>Disco livre</th>
          <th>Ativ. disco</th>
          <th>Latência</th>
          <th>Freq.</th>
          <th>Temp. CPU</th>
        </tr>
      </thead>
      <tbody id="corpo"></tbody>
    </table>
  </div>
  <div class="paginacao">
    <button id="anterior">← Anterior</button>
    <span class="info" id="infoPagina"></span>
    <button id="proxima">Próxima →</button>
  </div>
</section>

<footer id="rodape"></footer>

<script>
const dados = __DADOS__;
const POR_PAGINA = 100;
let pagina = 0;

const fmtNum = (valor, casas = 1) =>
  valor === null || valor === undefined
    ? "—"
    : valor.toLocaleString("pt-BR", { minimumFractionDigits: casas, maximumFractionDigits: casas });

const fmtInt = valor =>
  valor === null || valor === undefined ? "—" : valor.toLocaleString("pt-BR");

const fmtBytes = valor => {
  if (valor === null || valor === undefined) return "—";
  const gb = valor / (1024 ** 3);
  return gb >= 1 ? fmtNum(gb, 1) + " GB" : fmtNum(valor / (1024 ** 2), 0) + " MB";
};

const fmtDataHora = iso => {
  const d = new Date(iso);
  return d.toLocaleDateString("pt-BR") + " " + d.toLocaleTimeString("pt-BR");
};

const fmtMinutos = minutos => {
  if (!minutos || minutos < 1) return "menos de 1 min";
  if (minutos < 60) return fmtNum(minutos, 0) + " min";
  return fmtNum(Math.floor(minutos / 60), 0) + " h " + fmtNum(minutos % 60, 0) + " min";
};

const classeCpu = cpu => (cpu >= 90 ? "cpu-alta" : cpu >= 70 ? "cpu-media" : "");

const fmtPct = (valor, casas = 1) => (valor == null ? "—" : fmtNum(valor, casas) + "%");

const textoRam = registro => {
  const partes = [];
  if (registro.ram != null) partes.push(fmtPct(registro.ram));
  if (registro.ramUsada != null) partes.push(fmtBytes(registro.ramUsada));
  return partes.length > 0 ? partes.join(" · ") : "—";
};

const rotulosMetricaCpu = {
  utility: "Utility (Gerenciador de Tarefas)",
  "processor-time": "Tempo de processador"
};

const sufixoLimiar = () => {
  const limiar = dados.resumo && dados.resumo.limiarCpu;
  return limiar === null || limiar === undefined ? "" : " " + fmtNum(limiar, 0) + "%";
};

const el = (tag, props = {}, texto) => {
  const node = document.createElement(tag);
  Object.entries(props).forEach(([chave, valor]) => node.setAttribute(chave, valor));
  if (texto !== undefined) node.textContent = texto;
  return node;
};

const renderMaquina = () => {
  const maquina = dados.maquina || {};
  const itens = [
    ["Sistema", maquina.sistemaOperacional || "—"],
    ["CPU", maquina.cpuNome || "—"],
    ["Núcleos", maquina.nucleos ? fmtInt(maquina.nucleos) : "—"],
    ["Threads", maquina.threads ? fmtInt(maquina.threads) : "—"],
    ["RAM", fmtBytes(maquina.ramTotalBytes)],
    ["Disco", fmtBytes(maquina.discoTotalBytes)]
  ];

  const dl = document.getElementById("maquina");
  itens.forEach(([rotulo, valor]) => {
    dl.appendChild(el("dt", {}, rotulo));
    dl.appendChild(el("dd", { title: valor }, valor));
  });
};

const renderTopProcessos = () => {
  const corpo = document.getElementById("topProcessos");
  const processos = dados.topProcessos || [];

  if (processos.length === 0) {
    const tr = el("tr");
    const td = el("td", { class: "vazio" }, "Sem amostragem de processos no período.");
    tr.appendChild(td);
    corpo.appendChild(tr);
    return;
  }

  processos.forEach((nome, indice) => {
    const tr = el("tr");
    tr.appendChild(el("td", { class: "pos" }, String(indice + 1)));
    tr.appendChild(el("td", { class: "nome", title: nome }, nome));
    corpo.appendChild(tr);
  });
};

const renderCards = () => {
  const resumo = dados.resumo || {};
  const cards = [
    ["Registros acima de" + sufixoLimiar(), fmtInt(resumo.registros)],
    ["Tempo acima de" + sufixoLimiar(), fmtMinutos(resumo.minutosRegistrados)]
  ];

  const container = document.getElementById("cards");
  cards.forEach(([rotulo, valor]) => {
    const card = el("div", { class: "card" });
    card.appendChild(el("div", { class: "rotulo" }, rotulo));
    card.appendChild(el("div", { class: "valor" }, valor));
    container.appendChild(card);
  });
};

const montarDetalhe = registro => {
  const tr = el("tr", { class: "detalhe", hidden: "hidden" });
  const td = el("td", { colspan: "8" });
  const conteudo = el("div", { class: "detalhe-conteudo" });

  const blocoDados = el("div", { class: "detalhe-bloco" });
  blocoDados.appendChild(el("h3", {}, "Métricas do registro"));
  const dl = el("dl");
  const linhas = [
    ["Data / hora", fmtDataHora(registro.ts)],
    ["CPU", fmtNum(registro.cpu, 2) + "%"],
    ["Métrica da CPU", rotulosMetricaCpu[registro.metricaCpu] || "não registrada"],
    ["RAM usada", fmtBytes(registro.ramUsada)],
    ["RAM", fmtNum(registro.ram, 1) + "%"],
    ["Disco livre", fmtBytes(registro.discoLivre)],
    ["Atividade do disco", fmtNum(registro.atividade) + "%"],
    ["Latência de leitura", registro.latencia == null ? "—" : fmtNum(registro.latencia, 0) + " ms"],
    ["Frequência da CPU", registro.freq == null ? "—" : fmtNum(registro.freq, 0) + " MHz"],
    ["Temperatura da CPU", registro.tempCpu == null ? "—" : fmtNum(registro.tempCpu) + " °C"],
    ["Temperatura do disco", registro.tempDisco == null ? "—" : fmtNum(registro.tempDisco) + " °C"],
    ["Duração da coleta", fmtInt(registro.coleta) + " ms"],
    ["CPU do PMW", fmtNum(registro.pmw, 2) + "%"]
  ];
  linhas.forEach(([rotulo, valor]) => {
    dl.appendChild(el("dt", {}, rotulo));
    dl.appendChild(el("dd", {}, valor));
  });
  blocoDados.appendChild(dl);
  conteudo.appendChild(blocoDados);

  const blocoProc = el("div", { class: "detalhe-bloco" });
  blocoProc.appendChild(el("h3", {}, "Processos naquele momento"));

  if (registro.proc && registro.proc.length > 0) {
    const tabela = el("table", { class: "proc-tabela" });
    const corpo = el("tbody");
    registro.proc.forEach(processo => {
      const linha = el("tr");
      linha.appendChild(el("td", {}, processo.nome));
      linha.appendChild(el("td", {}, fmtNum(processo.cpu) + "%"));
      corpo.appendChild(linha);
    });
    tabela.appendChild(corpo);
    blocoProc.appendChild(tabela);
  } else {
    blocoProc.appendChild(el("div", { class: "vazio" }, "Este registro não teve amostragem de processos."));
  }

  conteudo.appendChild(blocoProc);
  td.appendChild(conteudo);
  tr.appendChild(td);
  return tr;
};

const renderTabela = () => {
  const corpo = document.getElementById("corpo");
  corpo.innerHTML = "";

  const registros = dados.registros || [];
  const totalPaginas = Math.max(1, Math.ceil(registros.length / POR_PAGINA));
  pagina = Math.min(pagina, totalPaginas - 1);

  const inicio = pagina * POR_PAGINA;
  registros.slice(inicio, inicio + POR_PAGINA).forEach(registro => {
    const tr = el("tr", { class: "linha" });
    tr.appendChild(el("td", {}, fmtDataHora(registro.ts)));
    tr.appendChild(el("td", { class: classeCpu(registro.cpu) }, fmtPct(registro.cpu)));
    tr.appendChild(el("td", {}, textoRam(registro)));
    tr.appendChild(el("td", {}, fmtBytes(registro.discoLivre)));
    tr.appendChild(el("td", {}, fmtPct(registro.atividade)));
    tr.appendChild(el("td", {}, registro.latencia == null ? "—" : fmtNum(registro.latencia, 0) + " ms"));
    tr.appendChild(el("td", {}, registro.freq == null ? "—" : fmtNum(registro.freq, 0) + " MHz"));
    tr.appendChild(el("td", {}, registro.tempCpu == null ? "—" : fmtNum(registro.tempCpu) + " °C"));

    const detalhe = montarDetalhe(registro);
    tr.addEventListener("click", () => {
      detalhe.hidden = !detalhe.hidden;
      tr.classList.toggle("aberta", !detalhe.hidden);
    });

    corpo.appendChild(tr);
    corpo.appendChild(detalhe);
  });

  document.getElementById("infoPagina").textContent =
    "Página " + (pagina + 1) + " de " + totalPaginas +
    " · " + fmtInt(registros.length) + " registro(s) exibido(s) de " + fmtInt(dados.resumo.registros);

  document.getElementById("anterior").disabled = pagina === 0;
  document.getElementById("proxima").disabled = pagina >= totalPaginas - 1;
};

const renderAviso = () => {
  if (!dados.registrosOmitidos) return;
  const aviso = document.getElementById("aviso");
  aviso.className = "aviso";
  aviso.textContent =
    fmtInt(dados.registrosOmitidos) + " registro(s) não exibidos na tabela por limite de tamanho do arquivo. " +
    "Os totais consideram todos os registros.";
};

const renderRodape = () => {
  document.getElementById("rodape").textContent =
    "Gerado em " + fmtDataHora(dados.geradoEm) +
    " · lacunas significam painel de monitoramento fechado ou PMW parado, não CPU normal.";
};

document.getElementById("anterior").addEventListener("click", () => { pagina--; renderTabela(); });
document.getElementById("proxima").addEventListener("click", () => { pagina++; renderTabela(); });

renderMaquina();
renderTopProcessos();
renderCards();
renderAviso();
renderTabela();
renderRodape();
</script>
</body>
</html>
""";
}
