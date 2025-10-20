using System.Xml.Linq;
using SkiaSharp;

// ============================================================================
// TIPOS AUXILIARES
// ============================================================================

record GpsBounds(double MinLat, double MaxLat, double MinLon, double MaxLon);
record GpsPoint(double Latitude, double Longitude);

// ============================================================================
// CLASSE PRINCIPAL
// ============================================================================

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Validar argumentos
            if (args.Length < 2)
            {
                ExibirAjuda();
                return;
            }

            string caminhoVideo = args[0];
            string caminhoGpxOriginal = args[1];

            // Validar arquivos
            if (!File.Exists(caminhoVideo))
            {
                Console.WriteLine($"❌ Erro: Arquivo de vídeo não encontrado: {caminhoVideo}");
                return;
            }

            if (!File.Exists(caminhoGpxOriginal))
            {
                Console.WriteLine($"❌ Erro: Arquivo GPX não encontrado: {caminhoGpxOriginal}");
                return;
            }

            // Validar extensões
            if (!Path.GetExtension(caminhoVideo).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Erro: O arquivo de vídeo deve ter extensão .mp4");
                return;
            }

            if (!Path.GetExtension(caminhoGpxOriginal).Equals(".gpx", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Erro: O arquivo deve ser um .gpx válido");
                return;
            }

            // Construir caminho de saída
            string diretorioVideo = Path.GetDirectoryName(caminhoVideo);
            string nomeVideoSemExtensao = Path.GetFileNameWithoutExtension(caminhoVideo);
            string caminhoGpxSaida = Path.Combine(diretorioVideo, $"{nomeVideoSemExtensao}.gpx");

            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║        VIDEO GPS FILTER - Extrator de Rastreamento    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

            // 1. Obter duração do vídeo
            Console.WriteLine("📹 Extraindo informações do vídeo...");
            var arquivo = TagLib.File.Create(caminhoVideo);
            TimeSpan duracao = arquivo.Properties.Duration;
            DateTime dataCriacao = File.GetCreationTime(caminhoVideo);

            Console.WriteLine($"   ├─ Arquivo: {Path.GetFileName(caminhoVideo)}");
            Console.WriteLine($"   ├─ Duração: {duracao:hh\\:mm\\:ss} ({duracao.TotalSeconds:F2}s)");
            Console.WriteLine($"   └─ Data/Hora criação: {dataCriacao:yyyy-MM-dd HH:mm:ss}\n");

            // 2. Calcular intervalo de tempo
            DateTime horaInicio = dataCriacao;
            DateTime horaFim = dataCriacao.AddSeconds(duracao.TotalSeconds);

            Console.WriteLine("⏱️  Intervalo de filtro:");
            Console.WriteLine($"   ├─ Início: {horaInicio:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"   └─ Fim:    {horaFim:yyyy-MM-dd HH:mm:ss}\n");

            // 3. Ler e filtrar GPX
            Console.WriteLine("📍 Processando arquivo GPX...");
            Console.WriteLine($"   ├─ Arquivo: {Path.GetFileName(caminhoGpxOriginal)}");

            XDocument docGpx = XDocument.Load(caminhoGpxOriginal);
            XNamespace ns = docGpx.Root.Name.NamespaceName;

            // Encontrar todos os pontos de rastreamento
            var pontosOriginais = docGpx.Descendants(ns + "trkpt")
                .ToList();

            Console.WriteLine($"   ├─ Total de pontos: {pontosOriginais.Count}");

            // Filtrar pontos dentro do intervalo
            var pontosFiltrados = new List<XElement>();
            int pontosDescartados = 0;

            foreach (var ponto in pontosOriginais)
            {
                var elementoTempo = ponto.Element(ns + "time");
                if (elementoTempo != null && DateTime.TryParse(elementoTempo.Value, out DateTime tempoGps))
                {
                    if (tempoGps >= horaInicio && tempoGps <= horaFim)
                    {
                        pontosFiltrados.Add(new XElement(ponto));
                    }
                    else
                    {
                        pontosDescartados++;
                    }
                }
                else
                {
                    pontosDescartados++;
                }
            }

            Console.WriteLine($"   ├─ Pontos filtrados: {pontosFiltrados.Count}");
            Console.WriteLine($"   └─ Pontos descartados: {pontosDescartados}\n");

            if (pontosFiltrados.Count == 0)
            {
                Console.WriteLine("⚠️  Aviso: Nenhum ponto GPS encontrado no intervalo de tempo do vídeo!");
                return;
            }

            // 4. Gerar novo GPX
            Console.WriteLine("💾 Gerando arquivo GPX filtrado...");

            XNamespace nsGpx = "http://www.topografix.com/GPX/1/1";
            XNamespace nsXsi = "http://www.w3.org/2001/XMLSchema-instance";

            XDocument docSaida = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement(nsGpx + "gpx",
                    new XAttribute("version", "1.1"),
                    new XAttribute("creator", "VideoGpsFilter"),
                    new XAttribute(XNamespace.Xmlns + "xsi", "http://www.w3.org/2001/XMLSchema-instance"),
                    new XAttribute(nsXsi + "schemaLocation", "http://www.topografix.com/GPX/1/1 http://www.topografix.com/GPX/1/1/gpx.xsd"),
                    new XElement(nsGpx + "metadata",
                        new XElement(nsGpx + "desc", $"Filtrado de vídeo: {Path.GetFileName(caminhoVideo)}"),
                        new XElement(nsGpx + "time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"))
                    ),
                    new XElement(nsGpx + "trk",
                        new XElement(nsGpx + "name", $"Track do vídeo {nomeVideoSemExtensao}"),
                        new XElement(nsGpx + "trkseg",
                            pontosFiltrados
                        )
                    )
                )
            );

            // Salvar arquivo
            docSaida.Save(caminhoGpxSaida);

            // 4.1 Gerar imagem do mapa
            Console.WriteLine("🗺️  Gerando mapa visual...");

            string caminhoImagemMapa = Path.Combine(diretorioVideo, $"{nomeVideoSemExtensao}_mapa.png");

            var coordenadasOriginais = ExtrairCoordenadas(pontosOriginais, ns);
            var coordenadasFiltradas = ExtrairCoordenadas(pontosFiltrados, ns);

            GerarImagemMapa(coordenadasOriginais, coordenadasFiltradas, caminhoImagemMapa);
            Console.WriteLine($"   └─ Mapa salvo: {caminhoImagemMapa}\n");

            // 5. Exibir resumo
            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ✓ SUCESSO!                         ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

            Console.WriteLine("📊 Resumo do processamento:");
            Console.WriteLine($"   ├─ Pontos originais: {pontosOriginais.Count}");
            Console.WriteLine($"   ├─ Pontos filtrados: {pontosFiltrados.Count}");
            double percentual = (pontosFiltrados.Count * 100.0 / pontosOriginais.Count);
            Console.WriteLine($"   ├─ Percentual utilizado: {percentual:F2}%");
            Console.WriteLine($"   ├─ Arquivo GPX: {caminhoGpxSaida}");
            Console.WriteLine($"   └─ Mapa visual: {caminhoImagemMapa}\n");

            // Informações adicionais
            Console.WriteLine("📈 Detalhes dos pontos:");
            if (pontosFiltrados.Count > 0)
            {
                var primeiroPonto = pontosFiltrados.First();
                var ultimoPonto = pontosFiltrados.Last();

                var tempoPrimeiro = primeiroPonto.Element(ns + "time")?.Value ?? "N/A";
                var tempoUltimo = ultimoPonto.Element(ns + "time")?.Value ?? "N/A";
                var latPrimeira = primeiroPonto.Attribute("lat")?.Value ?? "N/A";
                var lonPrimeira = primeiroPonto.Attribute("lon")?.Value ?? "N/A";

                Console.WriteLine($"   ├─ Primeiro ponto: {tempoPrimeiro}");
                Console.WriteLine($"   │  └─ Coordenadas: ({latPrimeira}, {lonPrimeira})");
                Console.WriteLine($"   └─ Último ponto: {tempoUltimo}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro durante o processamento: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Detalhes: {ex.InnerException.Message}");
            }
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // MÉTODOS AUXILIARES
    // ========================================================================

    static List<GpsPoint> ExtrairCoordenadas(IEnumerable<XElement> pontos, XNamespace ns)
    {
        var coordenadas = new List<GpsPoint>();

        foreach (var ponto in pontos)
        {
            var latStr = ponto.Attribute("lat")?.Value;
            var lonStr = ponto.Attribute("lon")?.Value;

            if (latStr != null && lonStr != null &&
                double.TryParse(latStr, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(lonStr, System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out double lon))
            {
                coordenadas.Add(new GpsPoint(lat, lon));
            }
        }

        return coordenadas;
    }

    static GpsBounds CalcularBounds(List<GpsPoint> pontosOriginais, List<GpsPoint> pontosFiltrados)
    {
        var todosPontos = pontosOriginais.Concat(pontosFiltrados).ToList();

        if (todosPontos.Count == 0)
            throw new InvalidOperationException("Nenhum ponto GPS disponível para calcular bounds");

        double minLat = todosPontos.Min(p => p.Latitude);
        double maxLat = todosPontos.Max(p => p.Latitude);
        double minLon = todosPontos.Min(p => p.Longitude);
        double maxLon = todosPontos.Max(p => p.Longitude);

        // Adicionar margem de 10%
        double latMargin = (maxLat - minLat) * 0.1;
        double lonMargin = (maxLon - minLon) * 0.1;

        return new GpsBounds(
            minLat - latMargin,
            maxLat + latMargin,
            minLon - lonMargin,
            maxLon + lonMargin
        );
    }

    static SKPoint ConverterGpsParaPixel(GpsPoint ponto, GpsBounds bounds, int largura, int altura)
    {
        // Normalizar coordenadas (0-1)
        double normX = (ponto.Longitude - bounds.MinLon) / (bounds.MaxLon - bounds.MinLon);
        double normY = (ponto.Latitude - bounds.MinLat) / (bounds.MaxLat - bounds.MinLat);

        // Inverter Y (coordenadas de imagem começam do topo)
        normY = 1.0 - normY;

        // Converter para pixels
        float x = (float)(normX * largura);
        float y = (float)(normY * altura);

        return new SKPoint(x, y);
    }

    static void GerarImagemMapa(
        List<GpsPoint> pontosOriginais,
        List<GpsPoint> pontosFiltrados,
        string caminhoSaida,
        int largura = 1920,
        int altura = 1080)
    {
        // Calcular bounds
        var bounds = CalcularBounds(pontosOriginais, pontosFiltrados);

        // Criar superfície de desenho
        using var surface = SKSurface.Create(new SKImageInfo(largura, altura));
        var canvas = surface.Canvas;

        // Fundo transparente
        canvas.Clear(SKColors.Transparent);

        // Configurar estilos de desenho
        using var paintOriginal = new SKPaint
        {
            Color = SKColors.White,
            StrokeWidth = 3,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        using var paintFiltrado = new SKPaint
        {
            Color = SKColors.Yellow,
            StrokeWidth = 5,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true
        };

        // Desenhar traçado original (branco)
        if (pontosOriginais.Count > 1)
        {
            var path = new SKPath();
            var primeiroPonto = ConverterGpsParaPixel(pontosOriginais[0], bounds, largura, altura);
            path.MoveTo(primeiroPonto);

            for (int i = 1; i < pontosOriginais.Count; i++)
            {
                var ponto = ConverterGpsParaPixel(pontosOriginais[i], bounds, largura, altura);
                path.LineTo(ponto);
            }

            canvas.DrawPath(path, paintOriginal);
            path.Dispose();
        }

        // Desenhar traçado filtrado (amarelo)
        if (pontosFiltrados.Count > 1)
        {
            var path = new SKPath();
            var primeiroPonto = ConverterGpsParaPixel(pontosFiltrados[0], bounds, largura, altura);
            path.MoveTo(primeiroPonto);

            for (int i = 1; i < pontosFiltrados.Count; i++)
            {
                var ponto = ConverterGpsParaPixel(pontosFiltrados[i], bounds, largura, altura);
                path.LineTo(ponto);
            }

            canvas.DrawPath(path, paintFiltrado);
            path.Dispose();
        }

        // Adicionar legenda
        using var paintTexto = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };

        canvas.DrawText("━━ Traçado Original", 30, 40, paintTexto);
        paintTexto.Color = SKColors.Yellow;
        canvas.DrawText("━━ Trecho Filtrado", 30, 75, paintTexto);

        // Salvar imagem como PNG transparente
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(caminhoSaida);
        data.SaveTo(stream);
    }

    static void ExibirAjuda()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        VIDEO GPS FILTER - Extrator de Rastreamento    ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

        Console.WriteLine("Uso: VideoGpsFilter.exe <caminho_video.mp4> <caminho_gpx_original.gpx>\n");

        Console.WriteLine("Descrição:");
        Console.WriteLine("  Extrai informações de duração e data de criação de um vídeo MP4,");
        Console.WriteLine("  filtra pontos GPS de um arquivo GPX dentro do intervalo de tempo");
        Console.WriteLine("  do vídeo e gera um novo arquivo GPX com os pontos filtrados.\n");

        Console.WriteLine("Exemplos:");
        Console.WriteLine("  VideoGpsFilter.exe C:\\videos\\meu_video.mp4 C:\\gps\\rastreamento.gpx");
        Console.WriteLine("  VideoGpsFilter.exe \"D:\\Meus Vídeos\\viagem.mp4\" \"D:\\GPS\\track.gpx\"\n");

        Console.WriteLine("Saída:");
        Console.WriteLine("  O arquivo GPX filtrado será salvo no mesmo diretório do vídeo");
        Console.WriteLine("  com o mesmo nome, ex: C:\\videos\\meu_video.gpx\n");

        Console.WriteLine("Requisitos:");
        Console.WriteLine("  - Arquivo de vídeo em formato MP4");
        Console.WriteLine("  - Arquivo GPX com pontos de rastreamento contendo timestamps");
        Console.WriteLine("  - Timestamps em GPX devem estar no mesmo fuso que a criação do vídeo\n");
    }
}
