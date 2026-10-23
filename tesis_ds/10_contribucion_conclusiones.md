# E10. Contribución y conclusiones

## Oración de contribución

Esta investigación aporta principios para decidir en qué fallo usar persistencia durable, identidad con efecto atómico, o consulta antes de reenviar, qué garantía produce cada uno y bajo qué condición externa deja de producirla.

Esa oración es la que el tesista formuló en E3, ahora limitada a los tres mecanismos que E9 pudo sostener con evidencia. No cubre el reintento de un fallo del intermediario, el reenvío cuando la consulta responde que la solicitud no existe, ni la conciliación cuando la consulta guarda silencio. La posición en el marco de Gregor y Hevner (2013) se mantiene como mejora: los mecanismos ya estaban descritos y el conocimiento nuevo es la evidencia comparada de cuándo hace falta cada uno.

## Principios de diseño

La plantilla es la de Wieringa (2014): objetivo, contexto, mecanismo y prescripción. En los cuatro contrastes, los mecanismos que no estaban bajo prueba permanecieron en la respuesta de la propuesta. Ningún principio autoriza a apagar los otros tres y esperar el mismo resultado.

### Principio 1. Aceptar y diferir durante la indisponibilidad

Para conservar una solicitud durante una indisponibilidad temporal del servicio externo, en un origen monolítico que envía un efecto de estado por mensajería asíncrona y puede retener ese efecto en su propio libro, el origen debe aceptarla, grabar la retención y el mensaje en la misma transacción, y no publicarlo mientras la ventana siga cerrada. El rechazo no deja un envío pendiente, de modo que la operación solo continúa si alguien la presenta otra vez. El mensaje grabado sobrevive al intervalo y el despacho, al reabrirse la ventana, no depende de esa segunda presentación.

En el laboratorio, el brazo que acepta y difiere despachó las 30 solicitudes dentro de los 10 segundos posteriores a la reapertura, cada una a partir de la única solicitud insertada por el ejecutor. El brazo que rechaza dejó el saldo en 1 000.00 y no retuvo fondos. La ventana era un indicador local, no un cierre calendario de cinco minutos, y nadie distinto del ejecutor pudo haber repetido la operación.

### Principio 2. Reservar la identidad y el efecto en la misma transacción

Para que un resultado repetido deje una sola fila de efecto de la misma identidad, cuando ese resultado puede entregarse más de una vez y dos aplicadores pueden leer la identidad antes de que cualquiera la reserve, la reserva y la fila de efecto deben confirmarse en la misma transacción. La comprobación separada permite que ambos vean la identidad libre y ambos inserten el efecto. La clave de la identidad, dentro de la transacción, revierte el segundo intento completo.

Las 30 pasadas atómicas dejaron una fila. Las 30 pasadas separadas dejaron dos, con una espera de 400 ms entre la lectura y la inserción. El saldo no bajó dos veces, porque la retención ya lo había reducido al aceptar. El principio cubre la fila de efecto, no un segundo descuento.

### Principio 3. Consultar antes de reenviar si el servicio puede responder por la misma identidad

Para no reenviar mientras se ignora si el servicio externo ya recibió la solicitud, cuando la confirmación inicial puede perderse después de que ese servicio ya la persistió y puede responder una consulta por la misma identidad, el origen debe esperar al menos 10 segundos, consultar, y no reenviar si la respuesta dice que ya fue recibida. El silencio de la confirmación inicial es compatible con que la solicitud ya esté del otro lado. En este laboratorio el reenvío inmediato de la misma identidad tampoco produjo un segundo débito, porque la cámara reconoció esa identidad. La consulta evita el reenvío. No demuestra, por sí sola, que el reenvío hubiera duplicado el efecto.

Las 30 consultas tuvieron una espera de al menos 10 segundos, cero reenvíos y un débito. Las 30 de reenvío inmediato tuvieron un reenvío, cero consultas, una sola fila en la cámara y un débito. El principio deja de aplicar si el servicio no consulta por identidad o no reconoce un segundo envío de esa identidad. Esas dos condiciones no se invirtieron en el experimento.

### Principio 4. Grabar el trabajo pendiente antes de publicar

Para retomar el despacho después de que el proceso origen termina entre la aceptación y la publicación, cuando el mensaje de una publicación directa vive solo en la memoria de ese proceso y el libro le sobrevive, la transferencia, la retención y el trabajo pendiente deben confirmarse juntos, y la publicación debe ocurrir después. Al reiniciar, el despachador encuentra un mensaje durable. La publicación directa ya no tiene ese mensaje, aunque la identidad siga registrada.

Las 30 pasadas con trabajo pendiente terminaron debitadas y ninguna pasó a conciliación. Las 30 de publicación directa no retomaron el despacho. La caída fue una salida ordenada después de confirmar la transacción, no una interrupción a mitad de una sentencia. En cinco pasadas sin fallo, la mediana de aceptación fue 97 ms en la publicación directa y 95 ms con trabajo pendiente. Esa diferencia no supera el 20% y no autoriza a decir que una respuesta sea más rápida.

### Principio 5. Marcar conciliación cuando ya no hay despacho recuperable

Para que una solicitud aceptada cuyo despacho automático ya no puede retomarse quede en un estado observable, cuando al arrancar hay fondos retenidos, no hay confirmación del publicador y no hay trabajo pendiente, el origen debe marcar conciliación y no debe debitar, liberar ni reenviar. No queda un mensaje que publicar. Debitar o liberar cerraría el negocio sin una respuesta del servicio externo. La conciliación conserva la identidad, el monto y la retención, y deja la operación visible para una comparación posterior.

Este principio sale del rediseño formativo. En V1 la publicación directa dejaba los fondos retenidos sin un estado de cierre. Las 30 pasadas sumativas de ese brazo terminaron en conciliación, con la retención vigente, sin débito y con el saldo en 975.00. No es la conciliación por silencio de una consulta: esa ruta no se evaluó.

## Condiciones de contorno

Los cinco principios se probaron en un origen y una cámara simulada, en la misma máquina, con SQL Server, RabbitMQ y una transferencia saliente sintética de 25.00. El servicio externo respondía la consulta y reconocía la identidad. Los otros mecanismos de la propuesta permanecían activos en cada contraste.

No se reclaman para una integración síncrona, para una lectura sin efecto de estado, para una acreditación entrante, ni para un destino que no pueda consultarse o que trate cada reenvío como una operación nueva. No se reclaman para un cierre de un banco en producción, ni para una caída que interrumpa una sentencia a medias, ni para una persona que decide repetir la transferencia. Tampoco se reclama que la fila duplicada de efecto mueva el saldo dos veces.

La transferibilidad es analítica. Otro equipo puede construir un artefacto distinto si el fallo que enfrenta es uno de estos cuatro y se mantienen las condiciones de arriba. No puede esperar el mismo resultado solo por copiar la instancia en otro contexto.

## Amenazas a la validez

El detalle y la mitigación están en `tesis_ds/08_evaluacion.md`. Aquí queda el impacto que condiciona los principios.

La validez de constructo está acotada por el laboratorio. I4 no observa a una persona, I2 cuenta filas de efecto y la caída es una salida controlada. Los principios 1, 2 y 4 deben leerse con esas operacionalizaciones. Una cámara real o un operador pueden apartarse de ellas.

La validez interna del principio 2 depende de la espera de 400 ms. Una comprobación más rápida podría no duplicar la fila. El brazo atómico no usa esa espera y dejó una fila en las 30 pasadas.

La validez externa no sale de este ejemplar. No hubo un segundo contexto ni una evaluación de campo. Una teoría de diseño con condiciones de contorno para varios contextos independientes excedería la evidencia y el nivel de maestría.

La validez de la conclusión es la de un resultado de laboratorio, 30 de 30 en cada brazo. No hay inferencia a la frecuencia de un banco.

## Trabajo futuro

Cuatro rutas quedaron escritas y sin evaluación: el rechazo final que libera la retención, el reenvío único cuando la consulta dice que la solicitud no existe, la conciliación cuando la consulta no responde, y el reintento acotado de un fallo transitorio del intermediario. I5 cumple el tope de 10 segundos y no tiene mediana ni percentil 95. Cualquiera de esas extensiones exige pasadas nuevas, sin mover los umbrales de E6.

Un segundo contexto, con otro equipo y otro servicio externo que no sea la cámara de este repositorio, sería el paso siguiente si se quisiera subir el nivel de la contribución. Esta tesis no lo reclama.

## Cierre de E10

Los principios tienen objetivo, contexto y mecanismo, y cada uno señala dónde deja de aplicar. Ninguno se apoya en una fila que E9 marcó como no evaluada. La contribución queda en nivel de clase, no en una teoría de diseño. E10 queda cerrada el 22 de octubre de 2026.
