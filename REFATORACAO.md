# Refatoração aplicada

## O que foi ajustado
- Alinhamento completo do projeto ao novo modelo de configuração:
  - `GameInstallPath`
  - `AppDataBasePath`
  - `AppDataPcPath`
  - `InstancesRootPath`
- Remoção da dependência do caminho legado `RuntimeTokenPath` do fluxo principal.
- Inclusão de migração automática de `RuntimeTokenPath` para `AppDataPcPath` ao carregar configurações antigas.
- Centralização da resolução e normalização de caminhos em `PathLayoutService`.
- Recriação do projeto apenas com o código-fonte necessário.
- Exclusão lógica dos artefatos gerados em build (bin/obj, `.g.cs`, `.baml`, `.cache`, `.deps.json`, `.runtimeconfig.json`, executáveis e PDBs) do projeto refatorado.

## Estrutura mantida
- `MainWindow`: navegação principal.
- `SetupWindow`: configuração de caminhos, criação de instância e captura de tokens.
- `LauncherWindow`: seleção e abertura das contas configuradas.

## Observações
- O projeto mantém `TokenRules` no arquivo de configuração. Como os nomes dos arquivos de sessão não foram enviados junto com a solicitação, a interface continua usando as regras salvas em `settings.json`.
- Se houver configuração antiga com `RuntimeTokenPath`, o carregamento migra o valor automaticamente para `AppDataPcPath` e tenta inferir `AppDataBasePath`.
