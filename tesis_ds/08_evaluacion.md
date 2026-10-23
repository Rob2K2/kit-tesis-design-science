# E8. Evaluación

## Función de la etapa

La evaluación tiene dos funciones. La primera es demostrar si cada respuesta arquitectónica cumple los criterios de E6. La segunda es producir evidencia para los principios de selección. El ciclo formativo ocurre primero y debe producir un rediseño documentado. El ciclo sumativo ocurre después, con 240 ejecuciones, y no modifica los umbrales.

## Estrategia FEDS

| Ciclo | Propósito | Paradigma | Contenido |
|:--|:--|:--|:--|
| Formativo | Orientar el rediseño | Artificial / técnico | Fallo local: publicación directa frente a trabajo pendiente |
| Sumativo | Valorar la contribución | Artificial / técnico | Cuatro escenarios × dos respuestas × 30 repeticiones |

No hay evaluación de campo. Esa ausencia limita la transferibilidad a las condiciones del laboratorio y ya está declarada en el perfil. Los criterios de E6 permanecen fijos.

## Evaluación formativa — fallo local

### Pregunta de diseño

¿El trabajo pendiente, escrito en la misma transacción de la aceptación, conserva la solicitud cuando el origen cae después de aceptar y antes de publicar?

### Evidencia observada el 19 de octubre de 2026

La evidencia procede de `verificar-v1` en E7e. Son cinco repeticiones internas por respuesta. No forman parte de las 240 ejecuciones sumativas.

| Respuesta | Pérdidas (I1 operativo) | Estado tras el reinicio |
|:--|:--|:--|
| Publicación directa | 5 de 5 | Transferencia y retención vigentes. No hay trabajo pendiente. En ocho segundos no hay publicación ni débito |
| Trabajo pendiente | 0 de 5 | El reinicio publica y debita la misma identidad |

Mediana de aceptación sin caída: 97 ms (directa) y 95 ms (trabajo pendiente). Diferencia dentro del 20% de I7. Con cinco pasadas no se afirma superioridad temporal.

### Hallazgos

1. El punto de caída distingue las dos respuestas. No hace falta revisar el punto de inyección.
2. La publicación directa conserva la transferencia y la retención, pero no conserva un trabajo despachable. El reinicio no puede continuar. Eso es la pérdida de la solicitud aceptada para el despacho automático.
3. El trabajo pendiente conserva ese despacho. I1 = 0 en esas cinco pasadas.
4. La publicación directa deja fondos retenidos sin estado terminal ni de conciliación. Eso incumple el espíritu de I3 y de RF-13: la operación aceptada queda en limbo, no en conciliación.
5. El umbral del 20% no se modifica.

### Debilidad que obliga al rediseño

La propuesta no falló en conservar el despacho. La debilidad formativa está en el tratamiento del limbo: cuando no hay trabajo pendiente, la solicitud aceptada permanece en fondos retenidos sin pasar a un estado conciliable. Un investigador o un operador no ve una señal explícita de que el despacho automático ya no es posible.

Sin ese cambio, la evaluación sumativa mediría I1 como «no debitada a tiempo», pero I3 no podría informar con claridad el destino de la respuesta directa. El rediseño no abandona la conservación de la solicitud ni el límite de sobrecarga.

## Rediseño V2

El tesista aceptó el rediseño el 22 de octubre de 2026. Al arrancar, el origen busca transferencias en `FondosRetenidos` sin fila en `TrabajoPendiente` y sin `PublicadorConfirmadoEn`, y las marca `Conciliacion`. No debita, no libera la retención y no reenvía.

### Regresión del fallo local

`verificar-v2` repitió el fallo local el 22 de octubre de 2026, con cinco pasadas por respuesta. No forman parte de las 240 ejecuciones.

| Respuesta | Resultado |
|:--|:--|
| Publicación directa | 5 de 5 terminaron en conciliación, con la retención vigente, sin débito y con el saldo en 975.00 |
| Trabajo pendiente | 5 de 5 terminaron debitadas y ninguna pasó a conciliación |

`verificar-v0` volvió a terminar con código 0 después del cambio. La reanudación del trabajo pendiente no quedó absorbida por la conciliación.

## Condiciones cumplidas antes de la sumativa

La sumativa empezó cuando ya se cumplía lo fijado en E7d. V2 estaba implementada y el fallo local se había repetido sobre ella. Cada ejecución cambió un solo mecanismo, mediante `CIERRE`, `EFECTO`, `RECUPERACION` o `PUBLICACION`. El ejecutor vació las colas y reinició el libro entre pasadas. La cámara respondió la consulta por identidad y, ante un reenvío de la misma identidad, no creó una segunda solicitud. Polly no intervino: el reintento ante fallos del intermediario quedó fuera de estas 240 pasadas.

## Evaluación sumativa

`verificar-sumativa` corrió el 22 de octubre de 2026. Son 30 repeticiones por brazo y ocho brazos, 240 ejecuciones. El registro está en `artefacto/resultados/sumativa.csv`. La columna `cumple` indica que el brazo produjo el comportamiento que le correspondía en el contraste, no que la respuesta de control sea deseable. No se modificó ningún umbral de E6.

| Escenario | Brazo | Pasadas | Qué se observó |
|:--|:--|:--|:--|
| Cierre | Rechazar | 30 de 30 | Rechazo sin retención y saldo en 1 000.00 |
| Cierre | Aceptar y diferir | 30 de 30 | Un solo débito, despacho entre 0 y 10 000 ms tras la reapertura |
| Resultado duplicado | Transacción única | 30 de 30 | Una fila de efecto y una identidad procesada |
| Resultado duplicado | Comprobación separada | 30 de 30 | Dos filas de efecto y una identidad procesada |
| Confirmación perdida | Consulta a los 10 s | 30 de 30 | Una consulta, ningún reenvío, un débito y una solicitud en la cámara |
| Confirmación perdida | Reenvío inmediato | 30 de 30 | Un reenvío de la misma identidad, ninguna consulta, un débito y una solicitud en la cámara |
| Caída local | Publicación directa | 30 de 30 | Conciliación, retención vigente, ningún débito y saldo en 975.00 |
| Caída local | Trabajo pendiente | 30 de 30 | Débito de la misma solicitud y ninguna conciliación |

I7 no se volvió a medir. Sigue siendo la mediana formativa de 97 ms y 95 ms, dentro del 20 %, y no forma parte de estas 240 pasadas.

La comprobación separada duplicó la fila de efecto. El saldo no bajó dos veces, porque la retención ya lo había reducido en la aceptación. I2, tal como quedó operacionalizado, cuenta filas de efecto. No mide un segundo descuento del saldo.

I4 quedó en cero en el brazo que acepta y difiere porque el ejecutor insertó una sola solicitud y esa solicitud llegó a débito. No se observó a una persona decidiendo si repetía la operación. En el brazo que rechaza, la solicitud no se conservó.

En la confirmación perdida, los dos brazos terminaron con un débito. La cámara reconoció la misma identidad, así que el reenvío inmediato no produjo un segundo efecto remoto. La diferencia medida es la política de recuperación: consultar y no reenviar, o reenviar al segundo sin consultar. La espera de la consulta fue de al menos 10 segundos en las 30 pasadas.

## Amenazas a la validez

La validez de constructo queda limitada por el laboratorio. La caída es una salida controlada del proceso, no una interrupción a mitad de una sentencia. Origen y cámara comparten la misma instancia de SQL Server. I4 no observa un reintento humano. I2 cuenta filas de efecto y no un segundo movimiento del saldo. La mitigación es declarar esas operacionalizaciones antes de leer los resultados. El impacto residual es que un tribunal no puede tratar estos indicadores como si midieran el comportamiento de una cámara real ni el de un operador.

La validez interna del duplicado depende de una espera de 400 ms entre la comprobación y la inserción, introducida para que dos consumidores vean la identidad libre. Las 30 pasadas reprodujeron dos efectos bajo esa espera. Queda abierto que una comprobación más rápida, sin esa ventana, no duplique. El brazo atómico no usa esa espera y dejó un solo efecto en las 30 pasadas.

La validez externa no sale del ejemplar local: transferencias salientes, cuatro fallos, RabbitMQ en el mismo equipo y una cámara que sí responde la consulta y sí reconoce la identidad. No hay evaluación de campo ni en la nube. La transferibilidad es analítica y se limita a esas condiciones.

La validez de la conclusión no admite inferencia a una población. El resultado es 30 de 30 en cada brazo, en un diseño determinista de laboratorio. No se calcula significación estadística ni se generaliza la frecuencia a un banco en producción.

## Estado de la puerta E8

La puerta E8 queda cerrada el 22 de octubre de 2026. Hay un ciclo formativo con rediseño documentado y un ciclo sumativo de 240 ejecuciones. Los criterios siguen siendo los de E6. La muestra de evaluación es el ejecutor automático, no el testimonio que informó el problema. Las amenazas quedan declaradas con su impacto residual.
