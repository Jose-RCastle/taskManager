# Especificación funcional 0.1

TaskManagerOS es un simulador didáctico, no un administrador de procesos reales. Mantiene plantillas de programas e instancias independientes, genera una planificación por tick con llegadas e I/O, reproduce accesos sobre una MMU paginada y conserva instantáneas suficientes para explicar cada decisión.

## Alcance

- Planificación: Round Robin, SJF y prioridad no expropiativos.
- Memoria: reemplazo Óptimo y NRU; identidad de página `(instancia, página)`.
- Configuración y validación de CPU, memoria y patrones `0R,1W`.
- Persistencia JSON de un proyecto completo y demostración inicial de diez procesos.
- WinForms programático: inicio, programas, ejecución, SO y simulación paso a paso.
- Métricas: tiempos, referencias, hits, fallos y tasas seguras.

Quedan como extensiones: multicolas, sorteo, garantizada, FIFO, segunda oportunidad, reloj y LRU. No se presentan como funciones disponibles.

