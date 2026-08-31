# 🎯 OddsPulse Pro - Real-Time Betting Odds Movement & Parlay Hub

Plataforma profesional para el seguimiento en tiempo real de movimientos de momios, alertas de dinero inteligente (*Reverse Line Movement* y *Steam Moves*) y constructor interactivo de apuestas combinadas (Parlays) desarrollada en **C# (.NET ASP.NET Core)** y **SignalR WebSockets**.

![OddsPulse Preview](https://img.shields.io/badge/Status-Active-00f59b?style=for-the-badge)
![.NET Version](https://img.shields.io/badge/.NET-10.0-512bd4?style=for-the-badge&logo=dotnet)
![SignalR](https://img.shields.io/badge/SignalR-Real--Time-00d2ff?style=for-the-badge)

---

## ⚡ Características Principales

- 🔄 **Motor de Movimiento de Momios en Segundo Plano**: Simulación reactiva mediante `BackgroundService` en C# que actualiza cuotas cada 2-4 segundos.
- 📡 **WebSockets con SignalR**: Transmisión sub-segundo de variaciones de cuotas, alertas de dinero inteligente y marcadores en vivo.
- 📊 **Boletos Públicos vs Dinero Real (RLM)**: Indicador visual de discrepancia de volumen para detectar apuestas de sindicatos profesionales (*Sharps*).
- 🧾 **Boleta de Parlays Interactiva**:
  - Cálculo instantáneo de momios combinados (Americano, Decimal y Probabilidad Implícita).
  - Bonificador progresivo **Vegas Boost** (+3% a +35% de ganancia extra).
  - Sugerencias automáticas de cobertura (*Hedging*).
- 📈 **Gráficos de Línea con Chart.js**: Visualización detallada de la evolución temporal de la cuota desde su apertura.
- 🏛️ **Comparativa Multi-Bookmaker**: Matriz de mejores cuotas entre Bet365, Caliente.mx, Pinnacle, DraftKings y FanDuel.
- 🧮 **Calculadoras Integradas**: Herramientas para **Valor Esperado (+EV)** y **Arbitraje / Surebets**.

---

## 🚀 Requisitos y Ejecución

### Requisitos
- [.NET SDK 8.0, 9.0 o 10.0+](https://dotnet.microsoft.com/download)

### Instalación y Ejecución Local
```bash
# 1. Clonar el repositorio
git clone <URL_DEL_REPOSITORIO>
cd Parlays

# 2. Restaurar dependencias y compilar
dotnet build

# 3. Iniciar el servidor
dotnet run --urls "http://localhost:5240"
```

Abre tu navegador en: `http://localhost:5240`

---

## 🛠️ Tecnologías

- **Backend**: C# .NET Core, ASP.NET Core Razor Pages, SignalR, Hosted Services.
- **Frontend**: HTML5, CSS3 Moderno (Cyber Obsidian Theme), JavaScript ES6, Chart.js, FontAwesome.
