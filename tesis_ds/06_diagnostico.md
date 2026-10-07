# E6. Diagnóstico con indicadores y criterios

## Función del diagnóstico

El diagnóstico cumple dos funciones de Design Science. La primera es aportar evidencia de que el problema existe antes de construir. La segunda es fijar la línea base y los criterios con los que se juzgará cada respuesta arquitectónica. Los valores esperados de este documento quedan comprometidos antes de la construcción; modificarlos después de observar resultados invalidaría la evaluación.

## Evidencia de existencia del problema

No existe acceso al sistema bancario histórico, a sus incidentes ni a métricas de producción. Por ello, no se inventará una frecuencia ni un costo monetario del caso recordado por el tesista.

La evidencia disponible tiene tres fuentes de distinta fuerza:

| Fuente | Qué acredita | Límite |
|:--|:--|:--|
| Testimonio profesional del tesista | En una integración saliente real ocurrieron resultados satisfactorios duplicados, cierres programados de aproximadamente cinco minutos y reenvíos iniciados por el usuario. | No cuantifica frecuencia ni impacto y no puede corroborarse con registros. |
| Contratos operativos de proveedores de pago | PayPal (2026) documenta reintentos con identidad estable cuando la respuesta es incierta. SIBS (s. f.-a, s. f.-b) documenta notificaciones asíncronas entregadas al menos una vez, duplicadas, retrasadas o desordenadas, y prescribe consulta de estado para recuperación. | Describen la clase de integración, no el sistema histórico del tesista. |
| Literatura y plataformas examinadas en E4 y E5 | Los fallos parciales, las reentregas y la necesidad de idempotencia están documentados para mensajería y flujos durables (Ding et al., 2023; Kleppmann & Riccomini, 2026; RabbitMQ, s. f.-b). | No mide la frecuencia del problema en instituciones financieras bolivianas. |

La combinación satisface el mínimo de una fuente de práctica y una fuente documental. No satisface una línea base organizacional cuantitativa. Esa ausencia es una limitación del estudio, no un dato que pueda estimarse.

## Línea base

La línea base cuantitativa será la variante de control de cada escenario, medida en el laboratorio con datos sintéticos y el mismo simulador. No se presentará como el desempeño histórico del banco.

| Escenario | Variante de control | Comportamiento esperado antes de medir |
|:--|:--|:--|
| Cierre programado | Rechazo inmediato al usuario | La solicitud no queda pendiente y el usuario debe repetirla. |
| Resultado duplicado | Comprobar existencia y después aplicar el efecto, en pasos separados | Dos entregas concurrentes pueden producir dos efectos. |
| Confirmación inicial perdida | Reenvío inmediato sin consulta | El efecto remoto puede duplicarse o el resultado puede quedar desconocido. |
| Fallo local | Publicar después de modificar el estado local, fuera de la misma transacción | La solicitud aceptada puede perderse al reiniciar. |

Los conteos de esa línea base se obtendrán durante la evaluación y no se usarán para reescribir los umbrales de esta sección.

## Indicadores

### I1. Pérdida de solicitudes aceptadas

- **Definición:** proporción de solicitudes confirmadas al usuario que, después del fallo o reinicio, no tienen un registro recuperable con su identidad.
- **Fórmula:** solicitudes aceptadas sin registro recuperable / solicitudes aceptadas.
- **Unidad:** proporción.
- **Fuente:** registro automático de cada ejecución.
- **Línea base:** desconocida en producción; se medirá en la variante de publicación directa.
- **Valor esperado de la variante durable:** 0.

### I2. Efectos de negocio duplicados

- **Definición:** cantidad de transferencias cuya identidad de negocio produjo más de un débito o más de una liberación definitiva.
- **Fórmula:** transferencias con más de un efecto definitivo / transferencias con al menos un resultado procesado.
- **Unidad:** proporción.
- **Fuente:** libro sintético de efectos y tabla de identidades procesadas.
- **Línea base:** desconocida en producción; se medirá en la variante de comprobación separada.
- **Valor esperado de la variante atómica:** 0.

### I3. Operaciones observables al cierre

- **Definición:** proporción de solicitudes aceptadas que terminan debitadas, rechazadas con retención liberada o marcadas para conciliación.
- **Fórmula:** solicitudes en estado terminal o conciliable / solicitudes aceptadas.
- **Unidad:** proporción.
- **Fuente:** estado final registrado.
- **Línea base:** se medirá en cada variante de control.
- **Valor esperado:** 1 en todas las variantes que declaren aceptación. Una variante que rechaza antes de aceptar no entra en el denominador.

### I4. Repetición manual durante el cierre

- **Definición:** cantidad de solicitudes que el simulador de usuario debe volver a enviar después de un rechazo por cierre.
- **Fórmula:** reenvíos manuales / solicitudes presentadas durante el cierre.
- **Unidad:** proporción.
- **Fuente:** registro del cliente simulado.
- **Valor esperado de la aceptación durable:** 0.
- **Valor esperado del rechazo inmediato:** puede ser mayor que 0 y constituye la línea base de comparación.

### I5. Tiempo hasta el despacho posterior

- **Definición:** tiempo transcurrido desde que el simulador declara la reapertura hasta que se inicia el primer intento de envío de cada solicitud pendiente.
- **Unidad:** segundos.
- **Fuente:** marcas de tiempo del despachador.
- **Valor esperado:** menor o igual a 10 segundos para cada solicitud pendiente.
- **Resumen:** se reportarán mediana, rango y percentil 95.

### I6. Espera antes de consultar

- **Definición:** tiempo desde que vence el plazo de recepción de 1B hasta que se inicia la consulta por identidad.
- **Unidad:** segundos.
- **Fuente:** marcas de tiempo del componente de recuperación.
- **Valor esperado:** 10 segundos en el laboratorio, confirmado por el tesista el 6 de octubre de 2026.
- **Justificación:** SIBS (s. f.-a) recomienda consultar cada 5 a 10 segundos en flujos de corta duración. Se adopta el límite superior de ese intervalo. Este plazo no es el mismo que I5: I5 mide el inicio del despacho después de la reapertura; I6 mide la espera después de perder 1B y antes de consultar el estado.

### I7. Sobrecarga del camino sin fallos

- **Definición:** incremento del tiempo de aceptación local de la variante con persistencia durable respecto de la variante que responde sin dejar trabajo pendiente.
- **Fórmula:** (mediana durable − mediana directa) / mediana directa.
- **Unidad:** proporción.
- **Fuente:** ejecuciones sin fallo inyectado, separadas de los cuatro escenarios.
- **Valor esperado:** menor o igual a 0,20, confirmado por el tesista el 6 de octubre de 2026.
- **Límite de interpretación:** mide solo la aceptación local, no la validación de la institución receptora ni el cierre de la transferencia.

## Criterios de éxito fijados antes de construir

1. I1 = 0 en la variante de conservación durable.
2. I2 = 0 en la variante de efecto atómico.
3. I3 = 1 para toda solicitud aceptada.
4. I4 = 0 cuando la respuesta elegida es aceptar y diferir.
5. I5 ≤ 10 segundos para cada solicitud pendiente.
6. I6 = 10 segundos antes de consultar tras la pérdida de 1B.
7. I7 ≤ 0,20 de sobrecarga en la aceptación local sin fallos.

Estos criterios no se ajustarán después de conocer los resultados. Si una variante no los cumple, el resultado se informará como incumplimiento.

## Declaración contra el ajuste posterior

Los umbrales anteriores se establecen el 6 de octubre de 2026, antes de diseñar la implementación definitiva y antes de ejecutar la evaluación. La línea base experimental podrá conocerse recién al medir las variantes de control; esa medición no autoriza a cambiar los valores esperados.

## Fuentes de contexto confirmadas

El tesista confirmó el 6 de octubre de 2026 el uso de la documentación operativa de PayPal y SIBS como evidencia de la clase de problema. Esas páginas no describen el banco histórico ni sustituyen una línea base de producción.

PayPal (2026) exige el encabezado `PayPal-Request-Id` para que un reintento con la misma identidad devuelva el estado de la solicitud anterior y no capture el pago otra vez. Si el encabezado se omite, PayPal duplica la solicitud. Dos solicitudes simultáneas con la misma identidad pueden hacer que la segunda falle.

SIBS (s. f.-b) entrega notificaciones de forma asíncrona, al menos una vez, sin orden garantizado. La misma notificación puede llegar varias veces y el comercio debe procesarla con `transactionID` de modo idempotente. SIBS (s. f.-a) indica consultar el estado cuando falta la notificación, y recomienda un intervalo de 5 a 10 segundos en flujos breves. Las acciones de negocio solo proceden ante un estado final.

## Cierre de E6

Cada indicador tiene definición operacional, fórmula o unidad, fuente y valor esperado fijado antes de construir. La línea base de producción no existe y se sustituye, de forma declarada, por las variantes de control del laboratorio. La evidencia del problema combina testimonio, literatura y contratos operativos de pago. E6 queda cerrada el 6 de octubre de 2026.
