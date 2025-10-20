using System.Xml.Linq;
using SkiaSharp;

// ============================================================================
// TIPOS AUXILIARES
// ============================================================================

record GpsBounds(double MinLat, double MaxLat, double MinLon, double MaxLon);
record GpsPoint(double Latitude, double Longitude);
record TileCoord(int X, int Y, int Zoom);
record TileBounds(int MinX, int MaxX, int MinY, int MaxY, int Zoom);

// ============================================================================
// CLASSE PRINCIPAL
// ============================================================================

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Validar argumentos
            if (args.Length < 2)
            {
                ExibirAjuda();
                return;
            }

            string caminhoEntrada = args[0];
            string caminhoGpxOriginal = args[1];

            // Validar arquivo GPX
            if (!File.Exists(caminhoGpxOriginal))
            {
                Console.WriteLine($"❌ Erro: Arquivo GPX não encontrado: {caminhoGpxOriginal}");
                return;
            }

            if (!Path.GetExtension(caminhoGpxOriginal).Equals(".gpx", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Erro: O arquivo deve ser um .gpx válido");
                return;
            }

            Console.WriteLine("╔════════════════════════════════════════════════════════╗");
            Console.WriteLine("║        VIDEO GPS FILTER - Extrator de Rastreamento    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");

            // Verificar se é arquivo ou diretório
            bool ehDiretorio = Directory.Exists(caminhoEntrada);
            bool ehArquivo = File.Exists(caminhoEntrada);

            if (!ehDiretorio && !ehArquivo)
            {
                Console.WriteLine($"❌ Erro: Caminho não encontrado: {caminhoEntrada}");
                return;
            }

            List<string> videosParaProcessar = new List<string>();

            if (ehDiretorio)
            {
                // Processar pasta
                Console.WriteLine($"📁 Modo: Processamento de pasta");
                Console.WriteLine($"   └─ Pasta: {caminhoEntrada}\n");

                var arquivosMp4 = Directory.GetFiles(caminhoEntrada, "*.mp4", SearchOption.TopDirectoryOnly);

                if (arquivosMp4.Length == 0)
                {
                    Console.WriteLine("❌ Erro: Nenhum arquivo .mp4 encontrado na pasta");
                    return;
                }

                videosParaProcessar.AddRange(arquivosMp4);
                Console.WriteLine($"📹 Encontrados {videosParaProcessar.Count} arquivo(s) .mp4\n");
            }
            else
            {
                // Processar arquivo único
                if (!Path.GetExtension(caminhoEntrada).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"❌ Erro: O arquivo de vídeo deve ter extensão .mp4");
                    return;
                }

                Console.WriteLine($"📹 Modo: Processamento de arquivo único");
                Console.WriteLine($"   └─ Arquivo: {Path.GetFileName(caminhoEntrada)}\n");

                videosParaProcessar.Add(caminhoEntrada);
            }

            // Processar cada vídeo
            int totalVideos = videosParaProcessar.Count;
            int videoAtual = 0;
            int sucessos = 0;
            int falhas = 0;

            foreach (var caminhoVideo in videosParaProcessar)
            {
                videoAtual++;

                if (totalVideos > 1)
                {
                    Console.WriteLine($"\n{'═',60}");
                    Console.WriteLine($"📹 Processando vídeo {videoAtual}/{totalVideos}: {Path.GetFileName(caminhoVideo)}");
                    Console.WriteLine($"{'═',60}\n");
                }

                bool sucesso = await ProcessarVideo(caminhoVideo, caminhoGpxOriginal);

                if (sucesso)
                    sucessos++;
                else
                    falhas++;
            }

            // Resumo final para múltiplos vídeos
            if (totalVideos > 1)
            {
                Console.WriteLine($"\n\n{'═',60}");
                Console.WriteLine("📊 RESUMO FINAL DO PROCESSAMENTO");
                Console.WriteLine($"{'═',60}");
                Console.WriteLine($"✅ Sucessos: {sucessos}");
                Console.WriteLine($"❌ Falhas: {falhas}");
                Console.WriteLine($"📁 Total processado: {totalVideos}");
                Console.WriteLine($"{'═',60}\n");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro fatal durante o processamento: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Detalhes: {ex.InnerException.Message}");
            }
            Environment.Exit(1);
        }
    }

    // ========================================================================
    // FUNÇÃO DE PROCESSAMENTO DE VÍDEO
    // ========================================================================

    static async Task<bool> ProcessarVideo(string caminhoVideo, string caminhoGpxOriginal)
    {
        try
        {
            // Construir caminho de saída
            string diretorioVideo = Path.GetDirectoryName(caminhoVideo);
            string nomeVideoSemExtensao = Path.GetFileNameWithoutExtension(caminhoVideo);
            string caminhoGpxSaida = Path.Combine(diretorioVideo, $"{nomeVideoSemExtensao}.gpx");

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
                Console.WriteLine("⚠️  Aviso: Nenhum ponto GPS encontrado no intervalo de tempo do vídeo!\n");
                return false;
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

            await GerarImagemMapa(coordenadasOriginais, coordenadasFiltradas, caminhoImagemMapa);
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

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro durante o processamento de {Path.GetFileName(caminhoVideo)}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Detalhes: {ex.InnerException.Message}");
            }
            Console.WriteLine();
            return false;
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

    static GpsBounds CalcularBoundsCentralizados(List<GpsPoint> pontosCentrais, double zoomPercent = 0.25)
    {
        if (pontosCentrais.Count == 0)
            throw new InvalidOperationException("Nenhum ponto GPS disponível para calcular bounds");

        double minLat = pontosCentrais.Min(p => p.Latitude);
        double maxLat = pontosCentrais.Max(p => p.Latitude);
        double minLon = pontosCentrais.Min(p => p.Longitude);
        double maxLon = pontosCentrais.Max(p => p.Longitude);

        // Aplicar zoom: 0.25 = 25% de margem adicional ao redor do trecho
        // Valores menores = mais zoom (menos margem)
        double latRange = maxLat - minLat;
        double lonRange = maxLon - minLon;

        double latMargin = latRange * zoomPercent;
        double lonMargin = lonRange * zoomPercent;

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

    // ========================================================================
    // FUNÇÕES DE TILES DE MAPA (WEB MERCATOR)
    // ========================================================================

    static TileCoord LatLonParaTile(double lat, double lon, int zoom)
    {
        // Converter latitude/longitude para coordenadas de tile (Web Mercator)
        int n = 1 << zoom; // 2^zoom
        int x = (int)Math.Floor((lon + 180.0) / 360.0 * n);

        double latRad = lat * Math.PI / 180.0;
        int y = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);

        return new TileCoord(x, y, zoom);
    }

    static TileBounds CalcularTilesNecessarios(GpsBounds bounds, int zoom)
    {
        var topLeft = LatLonParaTile(bounds.MaxLat, bounds.MinLon, zoom);
        var bottomRight = LatLonParaTile(bounds.MinLat, bounds.MaxLon, zoom);

        return new TileBounds(
            Math.Min(topLeft.X, bottomRight.X),
            Math.Max(topLeft.X, bottomRight.X),
            Math.Min(topLeft.Y, bottomRight.Y),
            Math.Max(topLeft.Y, bottomRight.Y),
            zoom
        );
    }

    static async Task<SKBitmap?> BaixarTile(int x, int y, int zoom, HttpClient httpClient)
    {
        try
        {
            // Usar Esri World Imagery (satélite gratuito)
            string url = $"https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{zoom}/{y}/{x}";

            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var stream = await response.Content.ReadAsStreamAsync();
                return SKBitmap.Decode(stream);
            }
        }
        catch
        {
            // Ignorar erros de download individual
        }

        return null;
    }

    static (double lat, double lon) TileParaLatLon(int x, int y, int zoom)
    {
        // Converter coordenadas de tile de volta para lat/lon (canto superior esquerdo do tile)
        int n = 1 << zoom;
        double lon = x / (double)n * 360.0 - 180.0;
        double latRad = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * y / (double)n)));
        double lat = latRad * 180.0 / Math.PI;
        return (lat, lon);
    }

    static async Task RenderizarTilesDeFundo(SKCanvas canvas, GpsBounds bounds, int largura, int altura)
    {
        // Determinar o nível de zoom adequado baseado na área
        int zoom = DeterminarZoomOtimo(bounds, largura, altura);

        // Calcular quais tiles precisamos
        var tileBounds = CalcularTilesNecessarios(bounds, zoom);

        Console.WriteLine($"   ├─ Baixando tiles de mapa (zoom {zoom})...");

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", "VideoGpsFilter/1.0");

        int totalTiles = (tileBounds.MaxX - tileBounds.MinX + 1) * (tileBounds.MaxY - tileBounds.MinY + 1);
        int tilesDownloaded = 0;

        // Baixar e desenhar cada tile
        for (int tileY = tileBounds.MinY; tileY <= tileBounds.MaxY; tileY++)
        {
            for (int tileX = tileBounds.MinX; tileX <= tileBounds.MaxX; tileX++)
            {
                var tileBitmap = await BaixarTile(tileX, tileY, zoom, httpClient);
                if (tileBitmap != null)
                {
                    // Calcular posição do tile na imagem
                    var (tileLatTop, tileLonLeft) = TileParaLatLon(tileX, tileY, zoom);
                    var (tileLatBottom, tileLonRight) = TileParaLatLon(tileX + 1, tileY + 1, zoom);

                    var topLeft = ConverterGpsParaPixel(new GpsPoint(tileLatTop, tileLonLeft), bounds, largura, altura);
                    var bottomRight = ConverterGpsParaPixel(new GpsPoint(tileLatBottom, tileLonRight), bounds, largura, altura);

                    var destRect = SKRect.Create(
                        topLeft.X,
                        topLeft.Y,
                        bottomRight.X - topLeft.X,
                        bottomRight.Y - topLeft.Y
                    );

                    canvas.DrawBitmap(tileBitmap, destRect);
                    tileBitmap.Dispose();

                    tilesDownloaded++;
                }
            }
        }

        Console.WriteLine($"   ├─ Tiles baixados: {tilesDownloaded}/{totalTiles}");
    }

    static int DeterminarZoomOtimo(GpsBounds bounds, int largura, int altura)
    {
        // Calcular o zoom baseado na área coberta
        double latDiff = bounds.MaxLat - bounds.MinLat;
        double lonDiff = bounds.MaxLon - bounds.MinLon;

        // Estimar zoom - valores típicos entre 10-16 para áreas locais
        for (int zoom = 16; zoom >= 10; zoom--)
        {
            var tiles = CalcularTilesNecessarios(bounds, zoom);
            int numTiles = (tiles.MaxX - tiles.MinX + 1) * (tiles.MaxY - tiles.MinY + 1);

            // Limitar a ~20 tiles para não sobrecarregar
            if (numTiles <= 20)
                return zoom;
        }

        return 12; // Zoom padrão
    }

    static async Task GerarImagemMapa(
        List<GpsPoint> pontosOriginais,
        List<GpsPoint> pontosFiltrados,
        string caminhoSaida,
        int largura = 1920,
        int altura = 1080)
    {
        // Calcular bounds considerando todos os pontos (sem zoom)
        var bounds = CalcularBounds(pontosOriginais, pontosFiltrados);

        // Criar superfície de desenho
        using var surface = SKSurface.Create(new SKImageInfo(largura, altura));
        var canvas = surface.Canvas;

        // Fundo preto como fallback
        canvas.Clear(SKColors.Black);

        // Baixar e renderizar tiles de mapa de fundo
        await RenderizarTilesDeFundo(canvas, bounds, largura, altura);

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
            Color = SKColors.Red,
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

        // Desenhar traçado filtrado (vermelho)
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
        paintTexto.Color = SKColors.Red;
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

        Console.WriteLine("Uso:");
        Console.WriteLine("  Arquivo único: VideoGpsFilter.exe <caminho_video.mp4> <caminho_gpx.gpx>");
        Console.WriteLine("  Pasta inteira: VideoGpsFilter.exe <pasta_videos> <caminho_gpx.gpx>\n");

        Console.WriteLine("Descrição:");
        Console.WriteLine("  Extrai informações de duração e data de criação de vídeo(s) MP4,");
        Console.WriteLine("  filtra pontos GPS de um arquivo GPX dentro do intervalo de tempo");
        Console.WriteLine("  do(s) vídeo(s) e gera novos arquivos GPX com mapas de satélite.\n");

        Console.WriteLine("Modos de Operação:");
        Console.WriteLine("  • Arquivo único: Processa um arquivo .mp4 específico");
        Console.WriteLine("  • Pasta: Processa todos os arquivos .mp4 encontrados na pasta\n");

        Console.WriteLine("Exemplos:");
        Console.WriteLine("  # Processar um arquivo único");
        Console.WriteLine("  VideoGpsFilter.exe C:\\videos\\meu_video.mp4 C:\\gps\\rastreamento.gpx");
        Console.WriteLine();
        Console.WriteLine("  # Processar todos os vídeos de uma pasta");
        Console.WriteLine("  VideoGpsFilter.exe C:\\videos C:\\gps\\rastreamento.gpx");
        Console.WriteLine();
        Console.WriteLine("  # Com caminhos contendo espaços");
        Console.WriteLine("  VideoGpsFilter.exe \"D:\\Meus Vídeos\" \"D:\\GPS\\track.gpx\"\n");

        Console.WriteLine("Saída:");
        Console.WriteLine("  Para cada vídeo processado, serão gerados no mesmo diretório:");
        Console.WriteLine("  • [nome_video].gpx - Arquivo GPX filtrado");
        Console.WriteLine("  • [nome_video]_mapa.png - Imagem com mapa de satélite e traçados\n");

        Console.WriteLine("Requisitos:");
        Console.WriteLine("  - Arquivo(s) de vídeo em formato MP4");
        Console.WriteLine("  - Arquivo GPX com pontos de rastreamento contendo timestamps");
        Console.WriteLine("  - Timestamps em GPX devem estar no mesmo fuso que a criação do vídeo");
        Console.WriteLine("  - Conexão com internet (para download de tiles de mapa de satélite)\n");
    }
}
