# 0001. Roslyn para leer el código del jugador

**Estado:** aceptada, 6 de noviembre de 2026.

## Contexto

El jugador escribe C# dentro del juego. El GDD pide conservar la sintaxis real, separar la validación de la ejecución, obtener resultados deterministas y hacer una prueba técnica con los programas de los 5 niveles antes de decidir.

Un juego compilado no trae un compilador de C#, así que hace falta una forma de leer ese texto.

## Decisión

Usamos **Roslyn** (`Microsoft.CodeAnalysis.CSharp` 4.8.0) solo para **analizar** la sintaxis. Nunca compila ni ejecuta el código del jugador.

Encima de Roslyn, en el ensamblado `Codebound.Language`, hay tres piezas propias:

| Pieza | Función |
|---|---|
| `SyntaxErrorTranslator` | Convierte los errores técnicos de Roslyn en mensajes en español para principiantes |
| `ProgramValidator` | Acepta solo el subconjunto permitido y rechaza todo lo demás |
| `ProgramInterpreter` | Ejecuta el programa validado y genera la traza de llamadas |

El parser propio se descartó por decisión del proyecto. Con Roslyn no hay que mantener una gramática de C#, y la sintaxis de clases y colecciones ya queda reconocida para etapas posteriores de la campaña.

## Subconjunto permitido

- Llamadas a los métodos del catálogo del juego.
- `int nombre = valor;`
- `for` contado: `for (int i = 0; i < N; i++)`
- `if` y `else`, con condiciones `bool` o `<` entre valores `int`.
- Bloques y comentarios.

No hay `while` ni asignaciones libres, y el intérprete además corta a los 500 pasos (llamadas más vueltas de ciclo). Por eso ningún programa puede colgar el juego.

## Resultados de la prueba

Probado con Unity 6000.3.22f1, backend Mono:

| Comprobación | Resultado |
|---|---|
| Tests en Unity (EditMode) | 71 de 71 correctos |
| Los 5 programas del GDD en un build de PC, sin editor | Correctos, con la traza esperada |
| Mismo programa ejecutado dos veces | Traza idéntica |
| `System.IO.File.Delete();` en el build | Rechazado |
| Tiempo de análisis en el build | 100 ms la primera vez; entre 1 y 53 ms después |
| Peso de las DLLs de Roslyn | 13,84 MB |

## Consecuencias

- **A favor:** la sintaxis es la real de C# y no mantenemos una gramática.
- **En contra:** el proyecto suma 13,84 MB y la primera lectura tarda unos 100 ms.
- **Mensajes:** los errores más comunes tienen traducción propia. Los demás usan un mensaje genérico que señala el texto del problema. Al actualizar Roslyn hay que revisar esos códigos de error.
- **Roslyn queda aislado:** las DLLs están marcadas como referencia explícita, y solo `Codebound.Language` puede usarlas. El resto del juego no depende de sus tipos.

## Límites conocidos

- **IL2CPP sin probar:** ese módulo no está instalado. La prueba cubre Mono, que es el backend actual para PC. Si se cambia de backend, hay que repetirla.
- **Diferencia con C#:** se acepta `if (HasKey()) { int a = 1; } int a = 2;`, que C# rechaza porque la segunda declaración alcanza también al bloque anterior. No afecta a los 5 niveles.
- **Mecánica obligatoria:** exigir la mecánica de cada nivel se implementa en la tarjeta "Definir validacion del nivel".

## Nota técnica

Si se crean los `.meta` de las DLLs a mano, el campo `platformData` debe usar el formato de mapa que escribe Unity. Con el formato de lista, Unity no registra la DLL como plugin y no da ningún aviso. Lo más seguro es configurarlas con `PluginImporter`.
