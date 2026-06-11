# Astronomia - Simulador Orbital

Simulador do Sistema Solar feito em **MonoGame DesktopGL**, criado para estudar astronomia de forma visual, interativa e experimental.

O projeto combina dois modos principais:

- **Modo 2D superior**: simulação gravitacional N-corpos com editor de massa e velocidade.
- **Modo inclinado**: visual cinematográfico/educacional com shaders, profundidade, órbitas em perspectiva e leitura artística dos planetas.

![Modo inclinado do simulador](image/screenshot_4k_20260513_123439_876.png)

## Destaques

- Sol, planetas principais, Lua e Plutão.
- Plutão como planeta anão estudável, com órbita elíptica e bem inclinada no modo inclinado.
- Motor gravitacional N-corpos no modo 2D.
- Planetas afetam as órbitas uns dos outros.
- Lua incluída na simulação gravitacional.
- Órbita lunar inclinada no modo inclinado, mostrando que ela não fica exatamente no plano Terra-Sol.
- Sombras de eclipse entre Terra e Lua quando o alinhamento com o Sol fica favorável.
- Editor para massa, rotação e velocidade de translação no modo 2D.
- Rastros orbitais no modo 2D.
- Modo inclinado com profundidade visual, frente/atrás do Sol e ordenação de corpos.
- Shaders para Sol, planetas, anéis, fundo espacial e poeira solar.
- Planetas com volume guiado pela luz do Sol e camada visual desenhada por cima.
- Atmosferas mais vivas em planetas como Vênus, Terra, Urano e Netuno.
- Anéis de Saturno com textura procedural e sombra do planeta nos anéis.
- Aneis de Urano finos, escuros e inclinados, inspirados no sistema real.
- Cinturão principal de asteroides entre Marte e Júpiter.
- Cinturão de Kuiper como faixa fria e dispersa além de Netuno.
- Fundo inclinado com gradiente espacial, nebulosidade sutil, Via Lactea estilizada, estrelas e paralaxe.
- Labels discretos no modo inclinado.
- Tooltip ao passar o mouse sobre planetas no modo inclinado, com nome, distância atual e velocidade orbital.
- Painel de estudo com fase visível aproximada do planeta.
- Painel contextual no modo inclinado com grupo, distância atual, posição frente/atrás, excentricidade e inclinação orbital.
- Interface usa fonte Bahnschrift, com leitura mais técnica/espacial que a fonte padrão.
- Painéis de estudo quebram linhas automaticamente e recalculam a altura para evitar sobreposição de textos.
- Filtro de centro de massa no modo 2D.
- Captura de tela com `F2`.

## Como Rodar

Requisitos:

- .NET 9 SDK
- MonoGame Content Builder via pacote do projeto

Execute:

```powershell
dotnet restore
dotnet run
```

## Controles

| Ação | Controle |
| --- | --- |
| Pausar/continuar | `Espaco` |
| Aumentar/reduzir escala de tempo | `+` / `-` |
| Reiniciar tempo, zoom e camera | `R` |
| Soltar foco do planeta | `C` |
| Sair | `Esc` |
| Zoom | Scroll do mouse |
| Mover camera | Setas |
| Arrastar câmera | Botão direito do mouse |
| Selecionar planeta/Sol | Clique esquerdo |
| Alternar modo 2D/inclinado | Botão no canto inferior direito |
| Salvar print | `F2` |

Os prints são salvos em:

```text
image/
```

## Modos

### Modo 2D Superior

Modo voltado para simulação e estudo físico. Os corpos usam o motor gravitacional, com massas e velocidades editáveis.

Inclui:

- órbitas elípticas;
- rastros reais da simulação;
- centro de massa;
- editor de massa;
- editor de velocidade de translação;
- leituras de velocidade, aceleração, força gravitacional, distância ao centro de massa e energia orbital simplificada.

### Modo Inclinado

Modo voltado para leitura visual e apresentação. Ele usa composição com `RenderTarget2D` e shaders para criar uma cena mais cinematográfica.

Inclui:

- órbitas em perspectiva;
- passagem visual de corpos e órbitas na frente/atrás do Sol;
- labels discretos;
- fase visível estimada;
- poeira solar;
- poeira do plano orbital;
- Via Lactea estilizada;
- paralaxe no fundo;
- anéis de Saturno e Urano;
- cinturao principal de asteroides.

## Shaders

O projeto usa efeitos HLSL/MonoGame:

- `SpaceBackground.fx`: fundo espacial com gradiente, estrelas, nebulosidade, Via Lactea e vinheta.
- `SunGlow.fx`: disco e brilho estilizado do Sol.
- `SolarDust.fx`: poeira radial solar e poeira do plano orbital.
- `ToonPlanet.fx`: planetas com volume e acabamento desenhado.
- `SaturnRings.fx`: anéis de Saturno e Urano com estilos diferentes.
- `SoftCircleMask.fx`: máscara suave para profundidade visual atrás de corpos.
- `PassThrough.fx`: composição final.

## Estrutura

```text
Source/
  Camera/        Camera, zoom e foco
  Interaction/   Seleção por clique
  Models/        Dados de corpos e assets
  Rendering/     Renderização, shaders e primitivas
  Simulation/    Motor gravitacional e dados orbitais
  UI/            HUD, painel de estudo e editor

Content/
  Effects/       Shaders .fx
  UiFont.spritefont

image/
  Prints salvos com F2
```

## Objetivo

Este projeto não busca ser um simulador profissional de astronomia. A proposta é ser uma ferramenta de estudo visual: misturar conceitos reais, simulação interativa e uma apresentação bonita o suficiente para tornar o aprendizado mais intuitivo.
