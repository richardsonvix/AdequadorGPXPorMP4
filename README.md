# AdequadorGPXPorMP4 - Video GPS Filter

Ferramenta de linha de comando para filtrar pontos GPS de arquivos GPX baseado na duração e data de criação de vídeos MP4.

## Descrição

Este projeto extrai automaticamente as informações de duração e data de criação de um vídeo MP4, e filtra os pontos GPS de um arquivo GPX para manter apenas os pontos que correspondem ao intervalo de tempo em que o vídeo foi gravado. Útil para sincronizar rastreamento GPS com vídeos de ação, dashcams, drones ou qualquer gravação com GPS separado.

## Funcionalidades

- Leitura de metadados de vídeos MP4 (duração e data de criação)
- Análise e filtragem de arquivos GPX
- Extração de pontos GPS dentro do intervalo temporal do vídeo
- Geração de novo arquivo GPX filtrado
- **Geração automática de mapa visual (.jpg)** mostrando:
  - Traçado completo do GPX original em branco
  - Trecho filtrado (correspondente ao vídeo) em amarelo
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
AdequadorGPXPorMP4.exe <caminho_video.mp4> <caminho_gpx_original.gpx>
```

### Exemplos

```bash
# Exemplo básico
AdequadorGPXPorMP4.exe C:\videos\meu_video.mp4 C:\gps\rastreamento.gpx

# Com caminhos com espaços
AdequadorGPXPorMP4.exe "D:\Meus Vídeos\viagem.mp4" "D:\GPS\track.gpx"
```

### Saída

Os arquivos gerados serão salvos automaticamente no **mesmo diretório do vídeo** com o mesmo nome base:

- Entrada: `C:\videos\meu_video.mp4`
- Saída GPX: `C:\videos\meu_video.gpx`
- Saída Mapa: `C:\videos\meu_video_mapa.jpg`

### Exemplo de Saída do Programa

```
╔════════════════════════════════════════════════════════╗
║        VIDEO GPS FILTER - Extrator de Rastreamento    ║
╚════════════════════════════════════════════════════════╝

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
   └─ Mapa salvo: C:\videos\viagem_mapa.jpg

╔════════════════════════════════════════════════════════╗
║                    ✓ SUCESSO!                         ║
╚════════════════════════════════════════════════════════╝

📊 Resumo do processamento:
   ├─ Pontos originais: 5420
   ├─ Pontos filtrados: 932
   ├─ Percentual utilizado: 17.20%
   ├─ Arquivo GPX: C:\videos\viagem.gpx
   └─ Mapa visual: C:\videos\viagem_mapa.jpg
```

## Considerações Importantes

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

## Licença

Este projeto é disponibilizado para uso livre.

## Contribuições

Contribuições são bem-vindas! Sinta-se à vontade para:
- Reportar bugs
- Sugerir novas funcionalidades
- Enviar pull requests

## Autor

Desenvolvido para facilitar a sincronização de vídeos com rastreamento GPS.
