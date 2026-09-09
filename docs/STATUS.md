# Estado 0.1.1

**0.1.1 — Corrección visual, sincronización de estado y preparación de demostración**

| Hito | Estado |
|---|---|
| Documentación y solución | Completo |
| Motor y planificación | Completo |
| MMU y simulación | Completo |
| Persistencia y demo | Completo |
| Interfaz WinForms | Completo |
| Pruebas y revisión | 16 pruebas deterministas preparadas; revisión estática completa, ejecución pendiente (SDK no presente) |

La corrección conserva los algoritmos de 0.1 y estabiliza los layouts de Programas, Lista de ejecución y Simulación. Incluye Gantt real, marcos adaptables, tabla y métricas legibles, nombres de presentación amigables, finalización limpia e invalidación por revisión de cualquier simulación anterior.

El contenedor de corrección no incluye el comando `dotnet`; por ello `dotnet build TaskManagerOS.sln` y `dotnet run --project tests/TaskManagerOS.Tests` quedan como validaciones obligatorias en Windows con el SDK .NET 8.

### Flujo de demostración

1. Revisar **Programas**.
2. Revisar **Lista de ejecución**.
3. Configurar **Round Robin** y **Óptimo**.
4. Iniciar la simulación.
5. Avanzar algunos pasos.
6. Señalar estados y Gantt.
7. Mostrar marcos y tabla de páginas.
8. Finalizar y explicar métricas.
