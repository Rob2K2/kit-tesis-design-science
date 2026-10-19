# E7d. Plan de construcción y versiones

## Función de la etapa

Este plan fija el mínimo que debe construirse, el orden de las versiones y la condición para pasar a la evaluación sumativa. No ejecuta el código ni interpreta resultados. El rediseño permanece obligatorio: su contenido se conocerá al observar la versión formativa, no antes.

## Mínimo experimental

La instancia mínima es la que permite probar el fallo local y, después, los otros tres contrastes. Comprende el origen, la cámara simulada y el ejecutor; el libro en SQL Server; las cuatro colas en RabbitMQ; la identidad de transferencia; los estados de E7c; y un registro capaz de calcular los indicadores.

Quedan fuera de todas las versiones la interfaz gráfica, la instalación en contenedores, el mapeo ejecutable hacia AWS, la criptografía, las acreditaciones entrantes y cualquier fallo distinto de los cuatro ya elegidos.

## Versiones

| Versión | Tipo | Pregunta de diseño | Incluye | Excluye |
|:--|:--|:--|:--|:--|
| V0 | Exploratoria | ¿Los tres procesos aceptan una solicitud, publican, reciben la confirmación inicial y conservan el estado después de reiniciar el origen? | Libro, colas, camino de la propuesta sin fallos, reinicio del proceso y borrado de datos | Los cuatro contrastes, el punto de caída y las 240 ejecuciones |
| V1 | Experimental formativa | ¿El trabajo pendiente, escrito en la misma transacción de la aceptación, conserva la solicitud cuando el origen cae antes de publicar? | Las dos respuestas del fallo local, el punto de caída nombrado, el reinicio y el cálculo de I1 e I7 | Cierre, resultado duplicado y confirmación perdida como escenarios de evaluación |
| V2 | Rediseño | ¿Qué cambio corrige la debilidad observada en V1 sin abandonar la conservación de la solicitud ni el límite de sobrecarga? | La corrección documentada y la repetición del fallo local | Nuevos escenarios y cambios de los umbrales de E6 |
| V3 | Experimental sumativa | ¿Qué mecanismo es necesario en cada fallo, qué garantía produce y qué costo temporal introduce? | Los cuatro contrastes, 30 repeticiones por respuesta y el registro de I1 a I7 | Evaluación en un banco o en AWS |

V0 responde si el vehículo funciona. V1 responde si el principio de conservación es discriminante. V2 convierte el hallazgo en un cambio rastreable. V3 produce la evidencia de los cuatro fallos. No habrá una versión operacional en un contexto bancario real: la evaluación sigue siendo de laboratorio.

Si V1 no muestra ninguna debilidad, no se declarará superada. Se revisará el punto de caída hasta que las dos respuestas puedan diferenciarse. El rediseño no se sustituye por una afirmación de que la primera versión era suficiente.

## Qué entra en la primera versión

V0 y V1 se construyen antes de la predefensa. V1 solo implementa el contraste del fallo local. Los otros tres mecanismos permanecen en la respuesta de la propuesta, aunque todavía no se evalúan. Así la caída no se confunde con un rechazo, un duplicado o un reenvío.

Polly no entra en V0 ni en V1. Aparece en V3, limitado a un fallo transitorio del intermediario y con la misma identidad, como exige RC-05.

## Criterio de suficiencia

La evaluación sumativa no empieza hasta que se cumplan todas estas condiciones:

1. V2 tiene un cambio documentado a partir de V1 y el fallo local fue repetido sobre ese cambio.
2. Cada ejecución sumativa modifica un solo mecanismo.
3. El ejecutor reinicia el libro y las colas entre repeticiones.
4. El punto de caída detiene el origen después de aceptar y antes de publicar, y el proceso puede iniciarse de nuevo.
5. El registro permite calcular I1, I2, I3, I4, I5, I6 e I7 sin reinterpretar los umbrales.
6. La cámara responde la consulta por identidad y reconoce un reenvío con esa misma identidad.
7. Los resultados de V3 no se usan para modificar RF-01 a RF-16.

## Riesgos

| Riesgo | Efecto | Mitigación |
|:--|:--|:--|
| El plazo hasta inicios de noviembre no alcanza para los cuatro escenarios | La predefensa se presentaría sin rediseño | V1 se limita al fallo local. Los otros tres esperan a V3, después de la predefensa |
| La caída no ocurre en el punto previsto | I1 no distingue las dos respuestas | El ejecutor invoca un punto nombrado y reinicia el proceso. No se usa una caída aleatoria |
| Una configuración cambia más de un mecanismo | El resultado no puede atribuirse | Cada ejecución registra los cuatro parámetros y solo uno difiere del lado de la propuesta |
| El trabajo pendiente supera el 20% de sobrecarga | Incumplimiento de RF-16 | V2 debe reducir esa escritura sin dejar de conservar la solicitud. Si no lo logra, V3 informa el incumplimiento |
| La confirmación del publicador se confunde con la confirmación inicial | Desaparece el escenario de 1B perdida | V0 debe mostrar una ejecución en la que el intermediario confirmó y la confirmación inicial todavía no llegó |
| La tentación de añadir contenedores o AWS | Retrasa la evidencia | Esas tareas permanecen fuera de V0, V1, V2 y V3 |

Con unas 15 horas semanales, el resto de octubre alcanza para V0, V1 y V2 si no se agregan escenarios. La evaluación de 240 ejecuciones queda entre la predefensa y la defensa.

## Cierre de E7d

El mínimo, las cuatro versiones, sus preguntas, el criterio de suficiencia y los riesgos están definidos. El 18 de octubre de 2026 el tesista confirmó dos decisiones:

1. V1 contrasta solo el fallo local. Los otros tres escenarios quedan para V3.
2. Las 240 ejecuciones sumativas empiezan únicamente cuando se cumple el criterio de suficiencia.

E7d queda cerrada.
