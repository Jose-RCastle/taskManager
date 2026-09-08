# TaskManagerOS — reglas del repositorio

- Usar C# 12/.NET 8, nullable e implicit usings.
- El núcleo debe permanecer independiente de Windows Forms y de APIs de procesos reales.
- La UI WinForms se construye exclusivamente por código, con layouts adaptables y `AutoScaleMode.Dpi`.
- No agregar paquetes externos salvo necesidad demostrable; persistir con `System.Text.Json`.
- Mantener simulaciones y desempates deterministas y acompañar cambios de lógica con pruebas de consola.
- Actualizar `docs/STATUS.md` cuando cambie el alcance entregado.

