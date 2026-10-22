# Sistema Distribuido de Telemetría y Monitoreo de Productividad (Framework SPACE)

Sistema cliente-servidor desacoplado para la captura automatizada de telemetría de bajo nivel en estaciones de trabajo Windows y el análisis de productividad en equipos de desarrollo de software, fundamentado en el **Framework SPACE** (Actividad, Eficiencia y Flujo, y Señales de Rendimiento).

## 🛡️ Privacidad por Diseño (*Privacy by Design*)

Este proyecto fue concebido bajo estrictos lineamientos éticos de monitoreo en entornos corporativos:
* **Sin Keylogging:** Queda expresamente excluida la captura de pulsaciones de teclado o lectura de búferes de texto.
* **Sin Vigilancia Visual/Auditiva:** No se realizan grabaciones de pantalla continuas ni se accede a periféricos audiovisuales (cámara/micrófono).
* El monitoreo se limita **estrictamente a metadatos** de ventanas, procesos y consumo de recursos de hardware.

## ✨ Características Principales

* **Captura Automatizada de Bajo Nivel:** Extrae el foco de la ventana activa, enumera aplicaciones concurrentes y calcula el consumo diferencial de CPU y memoria RAM mediante llamadas nativas a la Win32 API (`user32.dll`).
* **Detección Determinista de Inactividad (FSM):** Máquina de Estados Finitos que evalúa el tiempo sin interacción física de hardware (`GetLastInputInfo`). Al superar el umbral, conmuta el registro al proceso sintético `idle.exe` (0% CPU).
* **Tolerancia a Fallos (*Store-and-Forward*):** Persistencia síncrona en una base de datos local SQLite y transmisión asíncrona en lotes (Batching HTTP) cada 5 minutos, garantizando resiliencia ante cortes de red.
* **Protección Recíproca Inter-Sesión (Watchdog):** Servicio de Windows en Sesión 0 (SYSTEM) que vigila la salud del Agente recolector en Sesión 1, reanimándolo ante cierres forzados.
* **Hardware-Based Policy Management:** Asignación de perfiles y listas de aplicaciones autorizadas basadas en la dirección física inmutable de la tarjeta de red (MAC Address).

## 🏗️ Arquitectura y Estructura del Monorepo

El proyecto unifica cuatro módulos principales utilizando una arquitectura híbrida de microservicios y clientes ligeros:

```text
Tesis1_MT/
├── MonitorProductividad.sln             # Solución unificada para los proyectos C#
├── MonitorProductividad.Agent/          # Agente recolector local (.NET 10 - Sesión 1)
├── MonitorProductividad.Watcher/        # Servicio guardián Watchdog (.NET Worker - Sesión 0)
├── MonitorProductividad.Server/         # Backend REST API central (ASP.NET Core Minimal APIs)
├── monitor-dashboard-ui/                # Panel analítico web (React 18 + Vite + Tailwind v4)
└── docker-compose.yml                   # Orquestación local para PostgreSQL 17
```

## 📊 Diagrama de Flujo y Componentes

```mermaid
flowchart TB
    classDef default fill:#1e1e24,stroke:#3a3d52,stroke-width:2px,color:#ffffff,font-family:Segoe UI,font-weight:bold;
    classDef session0 fill:#112233,stroke:#3b7bbd,stroke-width:3px,color:#3b7bbd,font-family:Segoe UI,font-weight:bold;
    classDef session1 fill:#1a2b1f,stroke:#4caf50,stroke-width:3px,color:#4caf50,font-family:Segoe UI,font-weight:bold;
    classDef central fill:#2b1b35,stroke:#9c27b0,stroke-width:3px,color:#d500f9,font-family:Segoe UI,font-weight:bold;

    subgraph SESION_1 ["SESIÓN 1 (ESCRITORIO DE USUARIO)"]
        direction TB
        Agent["MonitorProductividad.Agent<br>(Proceso de Telemetría)"]
        
        subgraph Trackers ["Módulos de Captura (.NET Core)"]
            T1["Window Tracker<br>(Win32 API)"]
            T2["Cálculo CPU<br>(Diferencial Real)"]
            T3["Idle Tracker<br>(Hardware Input)"]
        end
        
        DB[("SQLite Local<br>(Store-and-Forward)")]
    end
    class SESION_1 session1;
    class Agent,T1,T2,T3 default;

    subgraph SESION_0 ["SESIÓN 0 (ENTORNO NATIVO)"]
        direction TB
        Watcher["MonitorProductividad.Watcher<br>(Servicio de Windows)"]
        SCM["Service Control Manager<br>(SCM Windows)"]
        Watchdog["WatchdogWorker<br>(Bucle 10s)"]
    end
    class SESION_0 session0;
    class Watcher,SCM,Watchdog default;

    subgraph CENTRAL_BACKEND ["NUBE / RED CORPORATIVA"]
        Server["Servidor Central<br>(ASP.NET Core API)"]
        Postgres[("PostgreSQL 17<br>(JSONB Analytics)")]
        Dashboard["React UI<br>(Dashboard Vite)"]
    end
    class CENTRAL_BACKEND central;
    class Server,Postgres,Dashboard default;

    T1 & T2 & T3 --> Agent
    Agent -->|Guarda localmente| DB
    SCM --> Watcher --> Watchdog
    Watchdog -.->|Supervisa y reanima| Agent
    Agent ====>|"HttpClient (Async JSON)"| Server
    Server -->|Ingesta y Analytics| Postgres
    Dashboard ====>|Consume API REST| Server

    linkStyle 5 stroke:#ff9800,stroke-width:2px;
    linkStyle 6 stroke:#9c27b0,stroke-width:3px;
```

## 🚀 Guía de Despliegue Local (Desarrollo)

### 1. Base de Datos (PostgreSQL)
Levanta la infraestructura de persistencia mediante Docker:
```bash
docker compose up -d
```

### 2. Backend Central (API REST)
Aplica las migraciones de Entity Framework Core para construir las tablas (incluyendo soporte nativo `JSONB`) y ejecuta el servidor:
```bash
cd MonitorProductividad.Server
dotnet ef database update
dotnet run
```
*El servidor escuchará en el puerto configurado (ej. `http://localhost:5259`).*

### 3. Frontend Web (React + Vite)
Instala las dependencias de Node.js y arranca el entorno de desarrollo:
```bash
cd monitor-dashboard-ui
npm install
npm run dev
```
*El dashboard estará disponible en `http://localhost:5173`.*

### 4. Agente Local
Inicia el agente de escritorio para comenzar a capturar métricas y despacharlas al servidor central:
```bash
cd MonitorProductividad.Agent
dotnet run
```

## ☁️ Estrategia de Despliegue en Producción
El monorepo está diseñado para despliegues independientes por componente:
* **Frontend:** Preparado para *Vercel* configurando `monitor-dashboard-ui` como *Root Directory*.
* **Backend:** Preparado para *Koyeb* o *Render* configurando `MonitorProductividad.Server` como *Work Directory*.
* **Base de Datos:** Compatible con *Supabase* u otros proveedores de PostgreSQL gestionado.
