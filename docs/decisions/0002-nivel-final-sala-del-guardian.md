# 0002. Nivel final: la sala del guardián

**Estado:** aceptada, 12 de noviembre de 2026.

## Contexto

El slice tiene 5 niveles y el 5, "El guardián del núcleo", es el jefe y el último. El plan pide que el jefe tenga una "sala más grande" y que se sienta como un cierre.

La versión del GDD medía 5×3 (más chica que los niveles 2 y 3) y solo usaba la llave y dos puertas. Pedía `for` e `if/else`, pero dejaba afuera la variable `int`, los módulos, el núcleo, la puerta de energía y la terminal. Es decir que el nivel final no repasaba lo que el jugador aprendió en los niveles 1 a 4.

## Decisión

El nivel 5 pasa a ser una sala de 9×3, la más grande del slice. La entrada es la misma del GDD (llave y dos puertas) y después se recorren, en orden, todas las mecánicas anteriores.

```
x →   0   1   2   3   4   5   6   7   8
y=0   .   K   .   .   .   .   N   .   .
y=1   .   R   G   G   M   M   M   P   T
y=2   .   .   .   .   .   .   .   .   .
```

| Símbolo | Objeto | Celda | Viene del nivel |
|---|---|---|---|
| `R` | Inicio del robot | (1,1) | — |
| `K` | Llave | (1,0) | 5 (GDD) |
| `G` | Puertas con llave | (2,1), (3,1) | 5 (GDD) |
| `M` | Módulos | (4,1), (5,1), (6,1) | 3 |
| `N` | Núcleo, carga pedida 4 | (6,0) | 4 |
| `P` | Puerta de energía | (7,1) | 4 |
| `T` | Terminal | (8,1) | 1 y 2 |

**Condición de victoria:** las dos puertas abiertas, los tres módulos activos, el núcleo con carga 4, la puerta de energía abierta y la terminal activa. No se pide una posición final, porque activar la terminal ya obliga al robot a cruzar la puerta de energía.

**Conceptos obligatorios:** `for`, `if/else` y una variable `int` usada como argumento. Son todos los conceptos del slice, así que el nivel final pide todo lo que pidieron los niveles anteriores.

**Carga 4 y no 3:** con 4, el jugador no puede copiar el valor del nivel 4. Ni las pistas ni el consejo dan el número. Si el jugador usa `Interact()` al lado de la puerta, el mensaje "La puerta necesita que el núcleo tenga carga 4." le dice qué valor hace falta.

## Solución de referencia

```csharp
int pulses = 4;
for (int i = 0; i < 2; i++)
{
    if (HasKey())
    {
        OpenGate();
    }
    else
    {
        CollectKey();
        OpenGate();
    }
    MoveRight();
}
for (int i = 0; i < 3; i++)
{
    MoveRight();
}
ChargeCore(pulses);
Interact();
MoveRight();
Interact();
```

Recorrido verificado con las reglas de `GridWorld`:

1. Vuelta 1: `HasKey()` es `false`, se toma el `else`, se junta la llave en (1,0), se abre (2,1) y el robot pasa a (2,1).
2. Vuelta 2: `HasKey()` es `true`, se abre (3,1) y el robot pasa a (3,1).
3. El segundo `for` lleva al robot a (6,1) y enciende los tres módulos.
4. `ChargeCore(pulses)` deja el núcleo (6,0) en 4.
5. El primer `Interact()` abre la puerta de energía, porque la terminal (8,1) todavía no está al lado.
6. El robot pasa a (7,1) y el segundo `Interact()` activa la terminal.

## Errores típicos que la validación detecta

| Programa | Resultado |
|---|---|
| Sin `if/else` (juntar la llave antes del `for`) | Llega, pero "este nivel te pide decidir con un if y su else." |
| `ChargeCore(4)` con el número escrito | Llega, pero pide la variable `int` |
| `int pulses = 3;` (la carga del nivel 4) | Quedan pendientes la terminal, la carga del núcleo y la puerta de energía |
| Sin el último `MoveRight(); Interact();` | "La terminal sigue apagada." |

## Consecuencias

- El programa del nivel 5 deja de ser el del GDD. `TestLevels.Level5Program` y la solución del asset son el programa de arriba.
- Los tests de las mecánicas de llave y puertas siguen usando la entrada de la sala, que no cambió.
- Lo visual del jefe (cámara más abierta, luces rojo-ámbar que pasan a verde y la música) queda para las tarjetas de arte y audio.
