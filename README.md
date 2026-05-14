# Logistic Control Center (LCC)

Sistema de control logístico industrial diseñado para la gestión, monitoreo y trazabilidad de procesos operativos en tiempo real. LCC permite administrar distintos módulos como consumo de materiales, gestión de bins, lavado, paletizado y otros procesos críticos dentro de una operación productiva.

---

## Descripción General

Logistic Control Center es una aplicación de escritorio multiplataforma basada en .NET, que utiliza una interfaz web embebida para ofrecer una experiencia moderna sin depender de frameworks frontend complejos.

El sistema está diseñado bajo una arquitectura modular, escalable y orientada a procesos industriales, permitiendo integrar múltiples fuentes de datos (MySQL y SAP) y manejar grandes volúmenes de información con eficiencia.

---

## Features

- Modular architecture for scalable logistics operations
- Real-time data processing and visualization
- Integration with MySQL and SAP Business One
- Dynamic SPA frontend without frameworks
- Excel export functionality (ClosedXML)
- Inline editing with persistence
- Cross-platform desktop application (Windows / macOS)

---

## Arquitectura del Sistema

### Backend

- Lenguaje: C#
- Framework: .NET 8
- Patrón: Handler → Service → Repository
- Comunicación: JSON message bridge mediante Photino.NET
- Bases de datos:
  - MySQL (operacional)
  - SQL Server (SAP Business One)

### Frontend

- HTML, CSS, JavaScript (vanilla)
- Arquitectura SPA sin frameworks
- Carga dinámica de módulos
- Comunicación con backend vía `window.PhotinoBridge.send()`

### Runtime

- Photino.NET (WebView embebido)
- Compatible con Windows y macOS

---

## Estructura del Proyecto


LogisticControlCenter/
│
├── src/
│   ├── Backend/
│   │   ├── Config/
│   │   │   └── AppSettings.cs
│   │   ├── Modules/
│   │   │   ├── Altillo/
│   │   │   ├── Bins/
│   │   │   ├── BinsLavado/
│   │   │   ├── BinsPrint/
│   │   │   ├── ConsumoPapel/
│   │   │   └── Palets/
│   │   ├── Repositories/
│   │   ├── Services/
│   │   │   ├── DbService.cs
│   │   │   └── MessageRouter.cs
│   │   └── Program.cs
│   │
│   └── UI/www/
│       ├── core/
│       ├── modules/
│       ├── shared/
│       ├── uploads/
│       └── index.html
│
├── config.json
├── config.example.json
└── LogisticControlCenter.csproj


---

## Principales Módulos

### Consumo Papel

- Registro y control de consumo por lote
- KPIs diarios (consumo, tarjas, saldo)
- Edición en línea de estados y salidas
- Exportación a Excel

### Altillo

- Gestión de inventario intermedio
- Campos editables con persistencia
- KPIs operacionales
- Soporte para edición masiva

### Bins

- Control de movimientos de bins
- Registro de entradas y salidas

### Lavado de Bins

- Seguimiento de bins en proceso de lavado
- Integración con base de datos `control_bins`

### Palets

- Registro y trazabilidad de palets
- Integración con proceso de producción
- Exportación de datos

---

## Flujo de Comunicación

El sistema utiliza un patrón de mensajería interno:

Frontend → PhotinoBridge → MessageRouter → Handler → Service → Repository → DB

Cada acción sigue una convención:


modulo.accion


Ejemplos:
consumo.obtenerConsumos
palets.exportarExcel
altillo.guardarCambios


---

## Configuración

El sistema utiliza un archivo `config.json` para gestionar las conexiones:

```json
{
  "MySqlHost": "localhost",
  "MySqlUser": "usuario",
  "MySqlPassword": "password",
  "SapHost": "localhost",
  "SapDatabase": "DB_NAME",
  "SapUser": "usuario",
  "SapPassword": "password"
}

Base de Datos
MySQL

Bases utilizadas:

consumo_papel
control_bins

Operaciones típicas:

SELECT con filtros por fecha, código y lote
Paginación mediante LIMIT/OFFSET
Transacciones para actualizaciones masivas
SAP (SQL Server)

Consultas típicas:

Integración con tablas OBTN y OITM
Unión de múltiples bases productivas
Uso de READ UNCOMMITTED para rendimiento
Exportación a Excel
Librería: ClosedXML
Generación directa desde backend
Archivos exportados al escritorio del usuario
Instalación y Ejecución
Requisitos
.NET 8 SDK
MySQL accesible
SQL Server (SAP) accesible
Ejecutar en desarrollo
dotnet run
Publicar aplicación
dotnet publish -c Release -r win-x64 --self-contained true
Publicación en macOS
dotnet publish -c Release -r osx-x64 --self-contained true
Convenciones del Proyecto
Backend

Cada módulo implementa:

Handler
Service
Repository

Formato de respuesta estándar:
{
  "ok": true,
  "data": {},
  "error": null
}
Frontend
Sin frameworks
Controladores por módulo
Destrucción de eventos al cambiar de vista
Uso de clases CSS reutilizables globales
Buenas Prácticas Implementadas
Separación clara de responsabilidades
Arquitectura modular escalable
Minimización de dependencias externas
Control de estado en frontend
Manejo consistente de errores
Prevención de duplicación de eventos
Optimización de consultas SQL
Estado del Proyecto
Sistema operativo en producción
Múltiples módulos funcionales
Instalador para Windows operativo
Preparado para distribución en macOS
Arquitectura lista para escalar
Autor

Desarrollado por Diego Carrasco

Proyecto enfocado en soluciones logísticas industriales y automatización de procesos

# LCC
