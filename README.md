# AdequadorGPXPorMP4 - Video GPS Filter

Ferramenta de linha de comando para filtrar pontos GPS de arquivos GPX baseado na duração e data de criação de vídeos MP4.

## Descrição

Este projeto extrai automaticamente as informações de duração e data de criação de um vídeo MP4, e filtra os pontos GPS de um arquivo GPX para manter apenas os pontos que correspondem ao intervalo de tempo em que o vídeo foi gravado. Útil para sincronizar rastreamento GPS com vídeos de ação, dashcams, drones ou qualquer gravação com GPS separado.

## Funcionalidades

- Leitura de metadados de vídeos MP4 (duração e data de criação)
- Análise e filtragem de arquivos GPX
- Extração de pontos GPS dentro do intervalo temporal do vídeo
- Geração de novo arquivo GPX filtrado
- **Geração automática de mapa visual (.png)** mostrando:
  - Imagem de satélite de fundo (tiles Esri World Imagery)
  - Traçado completo do GPX original em branco
  - Trecho filtrado (correspondente ao vídeo) em vermelho destacado
  - Renderização otimizada com zoom automático
- Interface de linha de comando com feedback visual detalhado
- Validação de entrada e tratamento de erros robusto

## Requisitos Técnicos

### Para Compilar

- **Visual Studio 2022** (versão 17.14 ou superior)
  - Componente: ".NET desktop development"
- **.NET 9.0 SDK**
- **Dependências NuGet:**
  - TagLibSharp 2.3.0 (leitura de metadados de vídeo)
  - SkiaSharp 2.88.8 (geração de imagens do mapa)

### Para Executar

- **Sistema Operacional:** Windows 10/11, Linux ou macOS
- **.NET 9.0 Runtime** ou superior
- **Conexão com Internet** (para baixar tiles de mapas de satélite)
- Arquivo de vídeo no formato MP4
- Arquivo GPX com pontos de rastreamento contendo timestamps

## Como Compilar

### Opção 1: Visual Studio 2022

1. Clone ou baixe o repositório
2. Abra o arquivo `AdequadorGPXPorMP4.sln` no Visual Studio 2022
3. Restaure os pacotes NuGet:
   - Menu: `Tools > NuGet Package Manager > Restore NuGet Packages`
   - Ou clique com botão direito na solução e selecione `Restore NuGet Packages`
4. Compile o projeto:
   - Modo Debug: `Ctrl + Shift + B` ou `Build > Build Solution`
   - Modo Release: Selecione `Release` na barra de ferramentas e compile

O executável será gerado em:
- **Debug:** `bin\Debug\net9.0\AdequadorGPXPorMP4.exe`
- **Release:** `bin\Release\net9.0\AdequadorGPXPorMP4.exe`

### Opção 2: .NET CLI (Linha de Comando)

```bash
# Clonar o repositório (se aplicável)
git clone <url-do-repositorio>
cd AdequadorGPXPorMP4

# Restaurar dependências
dotnet restore

# Compilar em modo Debug
dotnet build

# Compilar em modo Release
dotnet build -c Release
```

### Publicação (Executável Independente)

Para criar um executável que não precisa do .NET Runtime instalado:

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

O executável será gerado em: `bin\Release\net9.0\[runtime]\publish\`

## Como Usar

### Sintaxe

```bash
# Processar um arquivo único
AdequadorGPXPorMP4.exe <caminho_video.mp4> <caminho_gpx_original.gpx>

# Processar todos os vídeos de uma pasta
AdequadorGPXPorMP4.exe <pasta_videos> <caminho_gpx_original.gpx>
```

### Modos de Operação

**1. Arquivo único**: Processa um vídeo .mp4 específico
**2. Pasta inteira**: Processa automaticamente todos os arquivos .mp4 encontrados na pasta

### Exemplos

```bash
# Processar um arquivo único
AdequadorGPXPorMP4.exe C:\videos\meu_video.mp4 C:\gps\rastreamento.gpx

# Processar todos os vídeos de uma pasta
AdequadorGPXPorMP4.exe C:\videos C:\gps\rastreamento.gpx

# Com caminhos contendo espaços
AdequadorGPXPorMP4.exe "D:\Meus Vídeos" "D:\GPS\track.gpx"
```

### Saída

Os arquivos gerados serão salvos automaticamente no **mesmo diretório do vídeo** com o mesmo nome base:

**Para arquivo único:**
- Entrada: `C:\videos\meu_video.mp4`
- Saída GPX: `C:\videos\meu_video.gpx`
- Saída Mapa: `C:\videos\meu_video_mapa.png` (imagem PNG com mapa de satélite)

**Para pasta (exemplo com 3 vídeos):**
- Entrada: `C:\videos\` (contendo video1.mp4, video2.mp4, video3.mp4)
- Saídas GPX: `video1.gpx`, `video2.gpx`, `video3.gpx`
- Saídas Mapas: `video1_mapa.png`, `video2_mapa.png`, `video3_mapa.png`

### Processamento em Lote

Ao processar uma pasta, o programa:
- Identifica automaticamente todos os arquivos .mp4
- Processa cada vídeo sequencialmente
- Exibe progresso individual para cada vídeo (X/Total)
- Continua processando mesmo se um vídeo falhar
- Exibe resumo final com estatísticas de sucessos e falhas

### Exemplo de Saída do Programa

**Processamento de arquivo único:**

```
╔════════════════════════════════════════════════════════╗
║        VIDEO GPS FILTER - Extrator de Rastreamento    ║
╚════════════════════════════════════════════════════════╝

📹 Modo: Processamento de arquivo único
   └─ Arquivo: viagem.mp4

📹 Extraindo informações do vídeo...
   ├─ Arquivo: viagem.mp4
   ├─ Duração: 00:15:32 (932.00s)
   └─ Data/Hora criação: 2024-10-15 14:30:00

⏱️  Intervalo de filtro:
   ├─ Início: 2024-10-15 14:30:00
   └─ Fim:    2024-10-15 14:45:32

📍 Processando arquivo GPX...
   ├─ Arquivo: rastreamento.gpx
   ├─ Total de pontos: 5420
   ├─ Pontos filtrados: 932
   └─ Pontos descartados: 4488

💾 Gerando arquivo GPX filtrado...
🗺️  Gerando mapa visual...
   ├─ Baixando tiles de mapa (zoom 14)...
   ├─ Tiles baixados: 12/12
   └─ Mapa salvo: C:\videos\viagem_mapa.png

╔════════════════════════════════════════════════════════╗
║                    ✓ SUCESSO!                         ║
╚════════════════════════════════════════════════════════╝

📊 Resumo do processamento:
   ├─ Pontos originais: 5420
   ├─ Pontos filtrados: 932
   ├─ Percentual utilizado: 17.20%
   ├─ Arquivo GPX: C:\videos\viagem.gpx
   └─ Mapa visual: C:\videos\viagem_mapa.png
```

**Processamento de pasta (múltiplos vídeos):**

```
╔════════════════════════════════════════════════════════╗
║        VIDEO GPS FILTER - Extrator de Rastreamento    ║
╚════════════════════════════════════════════════════════╝

📁 Modo: Processamento de pasta
   └─ Pasta: C:\videos

📹 Encontrados 3 arquivo(s) .mp4

============================================================
📹 Processando vídeo 1/3: video1.mp4
============================================================

[... processamento do vídeo 1 ...]

============================================================
📹 Processando vídeo 2/3: video2.mp4
============================================================

[... processamento do vídeo 2 ...]

============================================================
📹 Processando vídeo 3/3: video3.mp4
============================================================

[... processamento do vídeo 3 ...]


============================================================
📊 RESUMO FINAL DO PROCESSAMENTO
============================================================
✅ Sucessos: 3
❌ Falhas: 0
📁 Total processado: 3
============================================================
```

## Considerações Importantes

### Mapa de Satélite

O programa utiliza tiles de imagens de satélite do **Esri World Imagery** para renderizar o fundo do mapa:
- Os tiles são baixados automaticamente durante a geração da imagem
- Requer conexão com internet ativa
- O nível de zoom é calculado automaticamente baseado na área GPS
- Os tiles são gratuitos para uso não comercial (cortesia da Esri)
- A primeira geração pode demorar alguns segundos devido ao download

### Fuso Horário

Para que a filtragem funcione corretamente, é essencial que:
- A data de criação do arquivo de vídeo esteja correta
- Os timestamps no arquivo GPX estejam no mesmo fuso horário que a criação do vídeo
- Verifique se o relógio do dispositivo que gravou o vídeo estava sincronizado

### Formato dos Arquivos

- **Vídeo:** Deve ser um arquivo MP4 válido com metadados de duração
- **GPX:** Deve seguir o padrão GPX 1.1 com pontos de rastreamento (`<trkpt>`) contendo elementos `<time>`

## Estrutura do Projeto

```
AdequadorGPXPorMP4/
├── Program.cs                      # Código principal da aplicação
├── AdequadorGPXPorMP4.csproj      # Arquivo de projeto .NET
├── AdequadorGPXPorMP4.sln         # Arquivo de solução Visual Studio
└── README.md                       # Este arquivo
```

## Tecnologias Utilizadas

- **Linguagem:** C# 11
- **Framework:** .NET 9.0
- **Bibliotecas:**
  - TagLibSharp - Leitura de metadados de vídeo
  - SkiaSharp - Geração de imagens 2D e visualização de mapas
  - System.Xml.Linq - Manipulação de arquivos GPX (XML)

## Solução de Problemas

### "Arquivo de vídeo não encontrado"
Verifique se o caminho está correto e se o arquivo existe. Use aspas para caminhos com espaços.

### "Nenhum ponto GPS encontrado no intervalo"
- Verifique se o fuso horário do vídeo e do GPX coincidem
- Confirme se a data de criação do vídeo está correta
- Verifique se o arquivo GPX contém timestamps válidos

### Erro ao compilar
- Certifique-se de ter o .NET 9.0 SDK instalado
- Execute `dotnet restore` para restaurar as dependências
- Verifique se o Visual Studio está atualizado para versão 17.14+

### Mapa não renderiza ou aparece preto
- Verifique sua conexão com a internet
- Alguns firewalls podem bloquear o acesso aos tiles de mapa
- Verifique se o domínio `arcgisonline.com` está acessível
- Se o problema persistir, o fundo preto será usado como fallback

## Licença

Este projeto é disponibilizado para uso livre.

## Contribuições

Contribuições são bem-vindas! Sinta-se à vontade para:
- Reportar bugs
- Sugerir novas funcionalidades
- Enviar pull requests

## Autor

Desenvolvido para facilitar a sincronização de vídeos com rastreamento GPS.
