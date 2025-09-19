# ProjectManagerWeb

**Gerenciador de projetos web desenvolvido em Vue 3 (frontend) e .NET 9 (backend).**

---

## ⚠️ Atenção

Este projeto foi feito no modo *“go horse” / “vibe coding”* — ou seja: foi entregue rápido para atender a necessidade, sem seguir 100% das boas práticas de um dev sênior. Funciona bem, mas pode (e deve) ser evoluído. 🚀

---

## 📦 Tecnologias usadas

- **Frontend:** Vue 3  
  - Framework UI: **Vuetify**  
  - Gerenciamento de estado: **Pinia**  

- **Backend:** .NET 9  
- Integração: backend serve o build do frontend (`dist`) em produção  

---

## 🛠️ Como rodar localmente (dev)

1. Clone o repositório  
   ```bash
   git clone https://github.com/wallaceSW11/ProjectManagerWeb.git
   ```

2. Entre na pasta do projeto  
   ```bash
   cd ProjectManagerWeb
   ```

3. Instale dependências do frontend  
   ```bash
   cd frontend
   npm install
   ```

4. Rode o frontend em modo dev (porta **5173**)  
   ```bash
   npm start
   ```

5. Inicie o backend (porta **2024**)  
   ```bash
   cd ../backend
   dotnet restore
   dotnet run
   ```

6. Acesse:  
   - Frontend dev: [http://localhost:5173](http://localhost:5173)  
   - Backend dev: [http://localhost:2024/api](http://localhost:2024/api)  

---

## 📦 Build e produção

1. Gere o build do frontend  
   ```bash
   cd frontend
   npm run build
   ```

2. O backend já está configurado para exportar o frontend gerado na pasta `dist`.  
   Basta rodar o backend em ambiente produtivo que ele servirá o front junto.  

---

## 🔍 Estrutura do projeto

```
ProjectManagerWeb/
│
├── frontend/             # Vue 3 + Vuetify + Pinia
│   ├── src/
│   ├── public/
│   ├── package.json
│   └── dist/             # gerado após build
│
├── backend/              # .NET 9
│   ├── Controllers/
│   ├── Models/
│   ├── Program.cs
│   └── (outros arquivos de configuração)
│
└── README.md
```

---

## 📖 Workflow de desenvolvimento

O processo adotado é simples e prático:  

- Para cada **tarefa do sprint**, é feito um **clone separado do repositório**.  
- A pasta recebe o nome da tarefa + breve descrição.  
- Isso permite trabalhar de forma isolada em cada demanda, sem poluir o código principal.  
- Ao final, as alterações podem ser integradas ou apenas servir como base de referência para aquela tarefa.  

---

## ⚡ Automação para subir múltiplos projetos

Uma das grandes facilidades do sistema é que, **após uma configuração inicial**, você pode iniciar **3 ou mais projetos com apenas alguns cliques**.  

- No app, você configura cada repositório indicando quais projetos existem e como rodar cada um (ex: `npm start`, `dotnet run`).  
- Na tela, basta marcar os projetos que deseja levantar e confirmar.  
- O backend cuida de disparar os comandos via **PowerShell**, eliminando a necessidade de abrir várias pastas e terminais manualmente.  

👉 Assim, ao invés de navegar pasta por pasta e rodar os comandos, você seleciona os projetos no painel e deixa o sistema cuidar da execução.
