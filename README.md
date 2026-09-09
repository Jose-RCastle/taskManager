# TaskManagerOS 0.1.1

TaskManagerOS es una aplicación académica de Sistemas Operativos que permite observar, tick a tick, cómo un planificador asigna CPU y cómo una MMU traduce referencias y reemplaza páginas. **No es el Administrador de tareas de Windows**: trabaja únicamente con modelos ficticios guardados por la propia aplicación; no enumera, modifica ni finaliza procesos reales.

## Requisitos

- Windows 10/11 y .NET 8 SDK.
- Visual Studio 2022 17.8 o posterior, con la carga **Desarrollo de escritorio de .NET**.

La lógica y las pruebas (`net8.0`) son multiplataforma. La presentación usa Windows Forms (`net8.0-windows`) y se ejecuta en Windows.

## Arquitectura

- `src/TaskManagerOS.Core`: modelos, parser/validación, planificación, memoria, simulación y JSON; no depende de WinForms.
- `src/TaskManagerOS.WinForms`: shell, navegación, tema oscuro, vistas y estado de aplicación. Toda la composición es C# programático, sin `Designer.cs`.
- `tests/TaskManagerOS.Tests`: runner determinista con aserciones propias y código de salida no cero al fallar.
- `docs`: especificación, plan y estado verificable.

Clases centrales: `ProcessDefinition` es una plantilla reutilizable; `ProcessInstance` es una ejecución independiente; `CpuTimelineGenerator` produce la CPU por tick; `MemoryManager` aplica accesos con identidad compuesta; `SimulationEngine` integra ambos recorridos y genera `SimulationStep` y `SimulationMetrics`; `ProjectStore` persiste `ProjectData` con `System.Text.Json`.

## Compilar, ejecutar y probar

```powershell
dotnet build TaskManagerOS.sln
dotnet run --project src/TaskManagerOS.WinForms
dotnet run --project tests/TaskManagerOS.Tests
```

En Visual Studio: abra `TaskManagerOS.sln`, establezca `TaskManagerOS.WinForms` como proyecto de inicio y pulse **F5**. La primera ejecución crea `%LOCALAPPDATA%\TaskManagerOS\project.json` con cinco plantillas y diez instancias listas.

## Funciones y algoritmos

- Planificación funcional: Round Robin (quantum configurable), SJF no expropiativo y prioridad no expropiativa (menor número = mayor prioridad).
- Se respetan llegada, cola de listos, una sola CPU, expropiación RR, bloqueo E/S, desbloqueo y finalización; los empates son deterministas.
- MMU funcional: Óptimo con referencias futuras y NRU con clases 0–3, bits R/M y reinicio periódico de R.
- Validación legible de patrones como `0R,1W,2R`, páginas, ráfagas y configuración.
- Visualización paso a paso, reproducción automática, pausa, velocidad, reinicio, Gantt, marcos, tabla de páginas, estados, historial y métricas.
- CRUD de programas, lista repetible/reordenable/editable, configuración y restauración de demostración.

Preparados como ampliación, pero deliberadamente no habilitados: multicolas, sorteo, planificación garantizada, FIFO, segunda oportunidad, reloj y LRU.

## Demostración breve para el profesor

1. Revise **Programas** y sus referencias/bloqueo E/S.
2. Revise **Lista de ejecución** y sus instancias actuales.
3. En **Configuración SO**, seleccione Round Robin, quantum 2, cuatro marcos y Óptimo.
4. Abra **Emular MMU** e inicie la simulación.
5. Avance algunos pasos.
6. Señale los estados y los segmentos del Gantt.
7. Muestre simultáneamente los marcos y la tabla de páginas.
8. Finalice y explique las métricas; use **Reiniciar** para repetir exactamente el resultado actual.

## Persistencia y decisiones

El guardado es automático tras cada modificación. La configuración inicial usa quantum 2, cuatro marcos, ocho páginas virtuales, páginas de 4 KB, reinicio NRU cada cuatro ticks y reemplazo Óptimo. Un patrón de referencias se repite cíclicamente cuando la ráfaga supera su longitud, una decisión explícita para mantener ejemplos compactos y reproducibles.
