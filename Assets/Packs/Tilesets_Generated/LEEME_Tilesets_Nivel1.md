# POLTOR7 - Nivel 1 "El Fondo": tilesets

Todos los PNG tienen fondo transparente donde corresponde, sin separación entre celdas y sin márgenes: se cortan solo con la cuadrícula.

## Archivos, cuadrícula y ajustes de importación

| Archivo | Tamaño | Celda (Grid By Cell Size) | Celdas | Pixels Per Unit | Pivot |
|---|---|---|---|---|---|
| 01_Pisos_Terreno.png | 256x96 | 32 x 32 | 8 x 3 | 32 | Center |
| 02_Paredes_Fondos.png | 256x128 | 32 x 32 | 8 x 4 | 32 | Center |
| 03_Puertas.png | 384x96 | 32 x 96 | 12 x 1 | 32 | Bottom Center |
| 04_Objetos.png | 256x224 | 32 x 32 | 8 x 7 | 32 | Center |
| 05_Personajes.png | 768x256 | 64 x 64 | 12 x 4 | 32 | Bottom Center |
| 06_Jefe_ChatarreroAlfa.png | 896x128 | 128 x 128 | 7 x 1 | 40 | Bottom Center |

Con Pixels Per Unit = 32, cada tile de 32 px mide exactamente 1 unidad de Unity (1 celda del Grid del Tilemap). Si el tilemap de su 2D Game Kit usa otro tamaño de píxel por tile, cambie el Pixels Per Unit para que una celda mida 1 x 1 unidad; los dibujos no hay que tocarlos. El jefe va con 40 PPU para que mida unos 3 unidades de alto.

Ajustes en el Inspector de cada PNG: Texture Type = Sprite (2D and UI), Sprite Mode = Multiple, Filter Mode = Point (no filter), Compression = None, Generate Mip Maps desactivado.

## Cómo cortarlos y llevarlos al Tile Palette

1. Seleccione el PNG, aplique los ajustes de arriba y pulse Apply.
2. Abra Sprite Editor > Slice > Type = Grid By Cell Size > Pixel Size = la celda de la tabla > Slice > Apply.
3. Cree una paleta (Window > 2D > Tile Palette > Create New Palette) y arrastre el PNG ya cortado a la paleta; Unity crea los tiles.
4. Para pisos, paredes y fondos use una paleta por archivo; para el nivel use un Tilemap para el suelo (con Tilemap Collider 2D + Composite Collider 2D, capa Ground), otro para paredes y otro para el fondo (Order in Layer más bajo).
5. Puertas, objetos y personajes son sprites sueltos: arrástrelos a la escena o conviértalos en prefabs.

Los índices se leen por fila, de izquierda a derecha (el nombre que Unity pone a cada sprite acaba en ese número).

## 01_Pisos_Terreno (32x32)
- Fila 0: 0: piso_arriba_izq | 1: piso_arriba_1 | 2: piso_arriba_2 | 3: piso_arriba_der | 4: piso_arriba_solo | 5: peligro_izq | 6: peligro_centro | 7: peligro_der
- Fila 1: 8: piso_lado_izq | 9: relleno_1 | 10: relleno_2 | 11: piso_lado_der | 12: relleno_remaches | 13: rejilla_ventilacion | 14: tuberia_horizontal | 15: tuberia_vertical
- Fila 2: 16: techo_abajo_izq | 17: techo_abajo_1 | 18: techo_abajo_2 | 19: techo_abajo_der | 20: plataforma_fina_izq | 21: plataforma_fina_centro | 22: plataforma_fina_der | 23: pasarela_rejilla

Uso: la fila 0 es la capa superior del suelo; la fila 1, los laterales y rellenos; la fila 2, el borde inferior (techos) y las plataformas finas (Platform Effector 2D, capa OneWay).

## 02_Paredes_Fondos (32x32)
- Fila 0: 0: pared_lisa | 1: pared_junta | 2: pared_remaches | 3: pared_ventana | 4: pared_tuberia_h | 5: neon_cian_encendido | 6: neon_apagado | 7: pared_ventilacion
- Fila 1: 8: cartel_advertencia | 9: zocalo_inferior | 10: moldura_superior | 11: columna | 12: columna_franja_azul | 13: sombra_esquina | 14: cables | 15: escotilla
- Fila 2: 16: criogenia_arriba | 17: criogenia_medio_apagado | 18: criogenia_base | 19: criogenia_medio_encendido | 20: fondo_torre_diagonal | 21: fondo_torre_rejilla | 22: fondo_luces_lejanas | 23: fondo_pozo_elevador
- Fila 3: 24: escalera | 25: escalera_borde_superior | 26: fondo_rejilla | 27: conducto | 28: pared_grieta | 29: pared_mancha | 30: tuberia_vertical_fondo | 31: oscuro_total

Las celdas de escalera, rejilla y conducto tienen transparencia parcial; ponga la escalera en su propio Tilemap.

## 03_Puertas (32x96, 1 x 3 unidades)
- Fila 0: 0: puerta_normal_cerrada | 1: puerta_normal_abriendo | 2: puerta_normal_abierta | 3: puerta_tarjeta_azul_cerrada | 4: puerta_tarjeta_azul_abriendo | 5: puerta_tarjeta_azul_abierta | 6: puerta_seguridad_cerrada | 7: puerta_seguridad_abriendo | 8: puerta_seguridad_abierta | 9: puerta_elevador_cerrada | 10: puerta_elevador_abriendo | 11: puerta_elevador_abierta

Orden: cerrada, abriendo, abierta para cada tipo. Normal = sin tarjeta; tarjeta_azul = requiredKeyId card_blue; seguridad = Oficina de Seguridad (código 0714); elevador = salida del nivel.

## 04_Objetos (32x32)
- Fila 0: 0: caja_madera | 1: caja_acero | 2: barril | 3: taquilla_cerrada | 4: taquilla_abierta | 5: caja_suministros_cerrada | 6: caja_suministros_abierta | 7: panel_fragil
- Fila 1: 8: botiquin | 9: racion | 10: municion_P7 | 11: tarjeta_azul | 12: fusible_cobre | 13: llave_inglesa | 14: aguja_P7 | 15: modulo_hackeo
- Fila 2: 16: terminal_apagada | 17: terminal_encendida | 18: calendario_0714 | 19: nota | 20: bitacora | 21: checkpoint_apagado | 22: checkpoint_activo | 23: palanca_arriba
- Fila 3: 24: palanca_abajo | 25: boton_rojo | 26: boton_azul | 27: boton_verde | 28: boton_amarillo | 29: boton_rojo_encendido | 30: boton_azul_encendido | 31: boton_verde_encendido
- Fila 4: 32: boton_amarillo_encendido | 33: neon_azul | 34: neon_amarillo | 35: neon_rojo | 36: neon_verde | 37: neon_apagado | 38: lampara_encendida | 39: lampara_apagada
- Fila 5: 40: capsula_criogenia_arriba_izq | 41: capsula_criogenia_arriba_der | 42: letrero_7-07 | 43: letrero_7-08 | 44: letrero_advertencia | 45: ventilador_1 | 46: ventilador_2 | 47: chispas_1
- Fila 6: 48: capsula_criogenia_abajo_izq | 49: capsula_criogenia_abajo_der | 50: flecha_arriba | 51: flecha_abajo | 52: chispas_2 | 53: disparo_plasma_P7 | 54: impacto | 55: letrero_salida

Notas: la cápsula de criogenia ocupa 2x2 celdas (índices 40, 41, 48, 49). Botones: rojo = 0, azul = 1, verde = 2, amarillo = 3 del SequencePuzzle (solución Azul, Amarillo, Rojo, Verde = 1, 3, 0, 2). Palanca arriba = inclinada a la derecha; abajo = a la izquierda.

## 05_Personajes (64x64, pivot en los pies)
- Fila 0: 0: ike_reposo_0 | 1: ike_reposo_1 | 2: ike_reposo_2 | 3: ike_reposo_3 | 4: ike_correr_0 | 5: ike_correr_1 | 6: ike_correr_2 | 7: ike_correr_3 | 8: ike_correr_4 | 9: ike_correr_5 | 10: ike_salto | 11: ike_caida
- Fila 1: 12: chatarrero_reposo_0 | 13: chatarrero_reposo_1 | 14: chatarrero_caminar_0 | 15: chatarrero_caminar_1 | 16: chatarrero_caminar_2 | 17: chatarrero_caminar_3 | 18: chatarrero_caminar_4 | 19: chatarrero_caminar_5 | 20: chatarrero_ataque_0 | 21: chatarrero_ataque_1 | 22: chatarrero_ataque_2 | 23: chatarrero_golpeado
- Fila 2: 24: chatarrero_morir_0 | 25: chatarrero_morir_1 | 26: chatarrero_morir_2 | 27: dron_volar_0 | 28: dron_volar_1 | 29: dron_volar_2 | 30: dron_volar_3 | 31: dron_cargar | 32: dron_disparar | 33: dron_morir_0 | 34: dron_morir_1 | 35: dron_morir_2
- Fila 3: 36: brazo_wrench | 37: brazo_p7 | 38: brazo_shotgun | 39: brazo_rifle | 40: fogonazo_1 | 41: fogonazo_2 | 42: ike_morir_0 | 43: ike_morir_1 | 44: ike_morir_2 | 45: ike_trepar_0 | 46: ike_trepar_1 | 47: ike_golpeado

Animaciones sugeridas (12 fps): reposo 0-3, correr 0-5, caminar del chatarrero 0-5, volar del dron 0-3. Los brazos con arma (brazo_wrench, brazo_p7, brazo_shotgun, brazo_rifle) están dibujados mirando a la derecha con el pivote a la izquierda del sprite: colóquelos dentro de ArmPivot. El cuerpo de Ike se dibujó con los dos brazos; al apuntar, el brazo con arma se superpone al cuerpo, y si le estorba el brazo delantero del sprite puede taparlo con el brazo del arma o recortarlo en un editor de imágenes.

## 06_Jefe_ChatarreroAlfa (128x128, PPU 40)
- Fila 0: 0: alfa_reposo_0 | 1: alfa_reposo_1 | 2: alfa_caminar_0 | 3: alfa_caminar_1 | 4: alfa_ataque_garra_alzada | 5: alfa_ataque_garra_golpe | 6: alfa_carga

El motor trasero naranja es el punto débil (x2 de daño): póngale un collider hijo.

## Licencia
Todo el arte fue dibujado por código para este proyecto, sin material de terceros; puede usarlo libremente en POLTOR7. Declare en el CREDITS.txt que los sprites fueron generados con ayuda de IA.
