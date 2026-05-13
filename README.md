# Astronomia

Projeto MonoGame DesktopGL para simular orbitas do Sistema Solar em 2D.

## Como rodar

```powershell
dotnet restore
dotnet run
```

## Controles

- `Espaco`: pausar ou continuar a simulacao.
- `+` / `-`: aumentar ou reduzir a escala de tempo.
- `R`: reiniciar tempo, zoom e camera.
- `Scroll do mouse`: aproximar ou afastar.
- `Setas`: mover a camera.
- `Botao direito do mouse`: arrastar a camera.
- Botao `Centralizar Sol`: levar a camera de volta para o Sol.
- Botao `Filtros`: abrir filtros visuais, incluindo centro de massa.
- `Clique no Sol ou em um planeta`: aproximar a camera e abrir painel de estudo.
- Botao `Modo inclinado` / `Modo 2D superior`: alternar entre vista superior e vista inclinada.
- No painel 2D, arraste os sliders para editar massa, velocidade de rotacao e velocidade de translacao.
- `C`: soltar o foco do planeta selecionado.
- `Esc`: sair.

## O que ja existe

- Sol, planetas principais e Lua.
- Periodos orbitais proporcionais em dias terrestres.
- Orbitas inclinadas para facilitar a leitura visual.
- Campo de estrelas, painel de tempo e indicador de pausa.
- Selecao do Sol e dos planetas com camera acompanhando e painel de dados.
- Modo 2D superior como vista inicial, com simulacao gravitacional real em 2D e orbitas elipticas.
- Rastros dos planetas no modo 2D mostram o caminho real produzido pela simulacao; o modo inclinado usa orbitas em perspectiva, com profundidade visual e passagem na frente/atras do Sol.
- No modo inclinado, planetas e satelites sao desenhados em ordem de profundidade para melhorar a oclusao visual.
- No modo inclinado, planetas e luas variam levemente em brilho e tamanho conforme a profundidade visual.
- No modo inclinado, a Lua tem orbita visual propria em tom claro e brilho discreto quando esta na frente da Terra.
- No modo inclinado, os aneis de Saturno tambem respeitam profundidade, passando atras e na frente do planeta.
- Infraestrutura inicial de shaders adicionada com um efeito `PassThrough` carregado pelo Content Pipeline.
- Modo inclinado agora passa por `RenderTarget2D`, preparando targets de cena, orbitas, mascara de corpos e glow para composicao com shaders.
- Orbitas traseiras do modo inclinado sao compostas por shader com mascara circular suave ao redor do Sol.
- A orbita traseira da Lua no modo inclinado tambem usa shader de mascara suave ao redor da Terra.
- A parte traseira dos aneis de Saturno no modo inclinado usa shader de mascara suave ao redor do planeta.
- Sol do modo inclinado usa shader `SunGlow`, com disco grafico, gradiente simples, glow quente, textura superficial sutil e pulso discreto.
- Planetas e luas no modo inclinado usam shader `ToonPlanet`, com volume de esfera guiado pela direcao do Sol e uma camada grafica 2D por cima, com hemisferio oculto em sombra solida, rampa de cinco tons iniciando no lado iluminado, textura mais presente na luz, contorno sutil e sombra desenhada.
- Aneis de Saturno no modo inclinado usam shader procedural `SaturnRings`, com faixas, lacunas e textura radial.
- Zoom do modo 2D permite afastar mais para estudar as distancias proporcionais reais.
- No modo 2D, o painel dos planetas mostra somente massa, velocidade de rotacao e velocidade de translacao.
- Editor 2D altera massa e velocidade de translacao diretamente na simulacao gravitacional.
- Painel 2D tambem mostra leituras calculadas: velocidade atual, aceleracao, forca gravitacional, distancia ao centro de massa e energia orbital simplificada.
- Motor gravitacional N-corpos: planetas tambem perturbam as orbitas uns dos outros.
- Lua da Terra incluida no motor N-corpos, com massa, velocidade inicial, renderizacao e rastro proprios; sua distancia orbital e ampliada apenas no desenho para ficar visivel.
- Filtro de centro de massa mostra o baricentro calculado com todos os corpos fisicos.

## Estrutura

- `Source/Game1.cs`: ciclo principal do MonoGame e orquestracao.
- `Source/Models`: registros de dados do Sol, planetas, estrelas e texturas.
- `Source/Simulation`: estado do sistema, dados iniciais, calculo orbital visual e motor gravitacional 2D.
- `Source/Camera`: zoom, pan e foco em corpos selecionados.
- `Source/Rendering`: desenho do sistema solar e primitivas 2D.
- `Source/UI`: HUD e painel de estudo.
- `Source/Interaction`: selecao por clique.
