# POLTOR7 - Sprites 2D y animaciones

Estilo pixel art, fondo transparente, personajes mirando a la **derecha** (use `flipX` para la izquierda).
Pixels por unidad (PPU): **32**. Pivote en los pies (abajo-centro), salvo Dron y Espora (centro).

## Contenido
- `sheets/`  una hoja por personaje: **cada fila = una animacion**, cada columna = un cuadro.
- `strips/`  una tira horizontal por animacion (lista para cortar a mano).
- `frames/`  cada cuadro como PNG suelto.
- `preview/` GIF animado de cada personaje y vistas generales.
- `animations.json`  nombre, fila, cuadros, fps y si hace bucle de cada animacion.
- `Poltor7SpriteImporter.cs`  importador automatico para Unity.

## Personajes y animaciones
| Personaje | Celda | Animaciones (cuadros) |
|---|---|---|
| Ike Ramos (jugador) | 32x32 | Idle (4), Walk (6), Run (6), Jump (3), Shoot (3), Melee (4), Hurt (2), Die (6) |
| Dra. Mara Quiroz | 32x32 | Idle (4), Walk (6), Talk (4) |
| Raizal | 32x32 | Idle (4), Walk (6), Run (6), Attack (4), Hurt (2), Die (6) |
| Guardia Corrupto | 32x32 | Idle (4), Walk (6), Shoot (3), Hurt (2), Die (6) |
| Chatarrero | 32x32 | Idle (4), Walk (6), Attack (4), Hurt (2), Die (6) |
| Chatarrero Alfa (jefe) | 64x64 | Idle (4), Walk (6), Attack (5), Charge (4), Hurt (2), Die (6) |
| Cmdt. Vex / Titan-V (jefe final) | 48x48 | Idle (4), Walk (6), Shoot (4), Slam (5), Hurt (2), Die (6) |
| Dron Centinela | 32x32 | Idle (4), Move (4), Shoot (3), Hurt (2), Die (5) |
| Espora Explosiva | 32x32 | Idle (4), Move (4), Explode (6) |
| Guardian del Reactor (jefe) | 64x64 | Idle (4), Open (4), Fire (4), Hurt (2), Die (6) |
| Madre Raiz (jefe) | 96x96 | Idle (6), Attack (6), Hurt (2), Die (6) |

## Instalacion en Unity (automatica, recomendada)
La carpeta destino puede ser cualquiera dentro de `Assets`; el script la encuentra solo.
Ejemplo con su estructura (`Assets/Art` y `Assets/Scripts`):
1. Cree `Assets/Scripts/Editor/` (clic derecho > Create > Folder, nombre exacto `Editor`) y copie ahi `Poltor7SpriteImporter.cs`.
2. Cree `Assets/Art/Sprites2D/` y copie dentro **`animations.json` y la carpeta `sheets/`** (la carpeta `sheets` debe quedar junto al json).
3. Espere a que Unity compile e importe (barra abajo a la derecha).
4. Menu **Poltor7 > Importar sprites 2D**. Se crean sprites cortados, clips `.anim`, un `AC_<Nombre>.controller` por personaje y prefabs en `Sprites2D/Prefabs`.

Si sale "No se encontro animations.json": revise que `animations.json` y `sheets/` esten dentro de `Assets` y en la misma carpeta.
Requiere el paquete **2D Sprite** (Package Manager > Unity Registry > "2D Sprite").

## Parametros del Animator
- Animaciones en bucle (Walk, Run, Move, Talk): parametro **bool** con el mismo nombre. Ej: `animator.SetBool("Walk", true)`.
- Animaciones de una sola vez (Attack, Shoot, Melee, Jump, Hurt, Die, Explode, Open, Fire, Slam, Charge): parametro **trigger**. Ej: `animator.SetTrigger("Die")`.
- `Die` y `Explode` se quedan en el ultimo cuadro; las demas vuelven a Idle.

## Instalacion manual (sin script)
1. Importe `<Nombre>_sheet.png`: Texture Type = Sprite (2D and UI), Sprite Mode = Multiple, Pixels Per Unit = 32, Filter Mode = Point, Compression = None.
2. Abra **Sprite Editor > Slice > Type: Grid By Cell Size**, tamano = celda (32, 48, 64 o 96), Pivot = Bottom. Pulse Slice y Apply.
3. Arrastre los cuadros de una fila a la escena para crear el clip (Unity crea Animator y SpriteRenderer solos). Marque Loop Time en las que repiten.

## Notas
- Los cuadros blancos en Hurt/Die son un destello de golpe intencional.
- Los jefes son mas grandes: Chatarrero Alfa 64, Vex 48, Guardian del Reactor 64, Madre Raiz 96. Ajuste colliders segun el tamano.
- Todo fue generado por codigo (`gen.py`), por lo que son arte de partida: puede pulirlos en Aseprite o LibreSprite.
