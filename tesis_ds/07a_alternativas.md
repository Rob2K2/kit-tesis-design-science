# E7a. Solución conceptual y alternativas

## Función de la etapa

Esta etapa convierte la solución conceptual en alternativas que pueden construirse y compararse. La decisión no elige al ganador de la evaluación: elige qué respuestas se diseñan, cuáles sirven de control y cuáles se descartan antes de escribir código. Los criterios de E6 permanecen fijos.

## Tipología

El artefacto sigue siendo la composición ya declarada: un modelo arquitectónico de estados e intercambios, una instancia mínima en .NET Core y los principios de selección que la evaluación pueda sostener. Adoptar una plataforma completa sustituiría esa composición por una instanciación ajena. Dividir el monolito tampoco resuelve los cuatro fallos y queda fuera del campo de acción (Talaver y Vakaliuk, 2023).

## Alternativas descartadas para el conjunto

| Alternativa | Qué resolvería | Por qué no es la propuesta |
|:--|:--|:--|
| MassTransit, NServiceBus, CAP o Wolverine como artefacto | Atomicidad local y deduplicación de mensajes dentro de su propia plataforma | Ocultan el mecanismo. La evaluación no podría separar la decisión de aplicación del comportamiento del producto (MassTransit, s. f.-a; Particular Software, s. f.-a; DotNetCore.CAP, s. f.-a; Wolverine, s. f.-a). |
| Temporal, Dapr o Durable Task como artefacto | Reanudación del flujo después de un fallo del proceso | Reintentan actividades al menos una vez y siguen exigiendo idempotencia del efecto remoto (Temporal Technologies, s. f.; Dapr, s. f.-a; Microsoft, s. f.-a). |
| Saga con compensación | Deshacer un efecto remoto ya aplicado | El alcance no incluye compensar un débito en la institución destino. Un rechazo final solo libera la retención local (García-Molina y Salem, 1987, examinada y apartada del núcleo en E4). |
| Reintento genérico con Polly ante cualquier silencio | Repetir llamadas después de un tiempo límite | Polly no vuelve atómica la publicación ni idempotente el efecto remoto (Microsoft, s. f.-b). Aderaldo et al. (2025) muestran que el costo del reintento crece con la tasa de fallos. |

La instancia se construirá sobre SQL Server, RabbitMQ y Polly, que el tesista ya domina. Esas tecnologías son el vehículo, no la contribución.

## Alternativas por escenario

Cada fila de control es una respuesta que un equipo puede defender. No es una versión rota a propósito. La fila de propuesta es la que los principios provisionales de E4 indican diseñar en detalle.

### Cierre programado

| | Control | Propuesta |
|:--|:--|:--|
| Respuesta | Rechazar al usuario mientras la cámara está cerrada | Aceptar, retener fondos, persistir el trabajo y despacharlo al reabrir |
| Principio | Ninguno de los ocho exige rechazar | Aceptación durable y reintento clasificado |
| Supuesto | El usuario puede repetir la operación | El cierre es conocido y tiene reapertura observable |
| Costo | Casi nulo en el origen | Una escritura durable y un despachador |
| Riesgo | La solicitud no queda recuperable y I4 no puede ser 0 | Retener fondos de una solicitud que después se rechace |
| Criterio que discrimina | I4 e I1 | I4 = 0 e I5 ≤ 10 segundos |

### Resultado duplicado

| | Control | Propuesta |
|:--|:--|:--|
| Respuesta | Comprobar si la identidad existe y, en otro paso, debitar o liberar | Insertar la identidad y aplicar el efecto en la misma transacción, con restricción de unicidad |
| Principio | La comprobación separada no impide que dos procesos vean «no existe» antes de confirmar | Efecto idempotente (Ding et al., 2023) |
| Supuesto | Las entregas llegan de una en una | Dos entregas pueden solaparse |
| Costo | Dos sentencias simples | Una transacción corta y un índice único |
| Riesgo | Doble débito o doble liberación | Rechazar una entrega legítima si la identidad se reutiliza para otra transferencia |
| Criterio que discrimina | I2 | I2 = 0 |

### Confirmación inicial perdida

| | Control | Propuesta |
|:--|:--|:--|
| Respuesta | Reenviar 1A en cuanto vence el plazo de 1B | Esperar 10 segundos, consultar por la misma identidad y reenviar solo si el contrato remoto reconoce duplicados |
| Principio | El silencio no distingue «no llegó» de «llegó y se perdió la respuesta» | Incertidumbre explícita y consulta antes de repetir (SIBS, s. f.-a; PayPal, 2026) |
| Supuesto | La cámara no procesó 1A | La cámara puede haber procesado 1A |
| Costo | Un envío adicional | Una consulta y la conservación de la identidad |
| Riesgo | Un segundo efecto remoto si la identidad cambia o el destino no deduplica | Quedar en conciliación si no existe consulta ni reconocimiento de duplicados |
| Criterio que discrimina | I2 e I3 | I2 = 0, I3 = 1 e I6 = 10 segundos |

La bandeja de salida no cierra este escenario. Conserva la solicitud local, pero no informa si la cámara procesó 1A.

### Fallo local entre aceptación y despacho

| | Control | Propuesta |
|:--|:--|:--|
| Respuesta | Modificar el estado local y publicar después, fuera de la misma transacción | Escribir transferencia, retención y mensaje pendiente en una transacción; publicar solo después de confirmarla |
| Principio | La publicación directa puede sobrevivir al proceso o perderse con él, sin relación atómica con el débito futuro | Aceptación durable y confirmación observable del publicador (Kleppmann y Riccomini, 2026; RabbitMQ, s. f.-a) |
| Supuesto | El proceso no cae en el intervalo | El proceso puede caer después de aceptar y antes de publicar |
| Costo | Una publicación | Una fila pendiente y un despachador con confirmación |
| Riesgo | Perder la solicitud aceptada | Publicar dos veces si la confirmación del intermediario se pierde; la identidad del efecto debe absorber esa repetición |
| Criterio que discrimina | I1 | I1 = 0 e I7 ≤ 0,20 |

Este escenario es el de la evaluación formativa. Si la conservación transaccional no basta, el rediseño se documenta antes de la evaluación sumativa.

## Matriz de decisión

Escala cualitativa: alta, media o baja. «Novedad» no significa inventar el mecanismo; significa cuánto aporta la comparación al conocimiento ya buscado.

| Alternativa | Factibilidad | Novedad de la comparación | Costo de construcción | Riesgo técnico | Sustento |
|:--|:--|:--|:--|:--|:--|
| Rechazo durante el cierre | Alta | Media, porque reproduce el antecedente | Bajo | Bajo | Contraste con la aceptación durable |
| Aceptación durable y despacho diferido | Alta | Media | Medio | Medio | Principios 1 y 6 de E4 |
| Comprobación separada | Alta | Media | Bajo | Alto en concurrencia | Contraste con Ding et al. (2023) |
| Identidad y efecto atómicos | Alta | Media | Medio | Medio | Principio 3 de E4 |
| Reenvío inmediato | Alta | Media | Bajo | Alto si 1A fue procesado | Contraste con el principio 5 |
| Consulta y reenvío con la misma identidad | Media | Alta | Medio | Alto si el simulador no consulta | PayPal (2026); SIBS (s. f.-a); principios 2, 4 y 5 |
| Publicación directa | Alta | Media | Bajo | Alto ante caída local | Contraste con el principio 1 |
| Trabajo pendiente en la misma transacción | Alta | Media | Medio | Medio | Principios 1 y 7; RabbitMQ (s. f.-a) |

La factibilidad media de la consulta depende de una condición ya declarada: el simulador debe ofrecer consulta por identidad y debe reconocer un reenvío con esa misma identidad. Sin esa condición, la propuesta de confirmación perdida solo puede llegar a conciliación, y así deberá informarse.

## Decisión

Diseñar como propuesta la combinación de cuatro respuestas: aceptación durable con despacho diferido, identidad y efecto atómicos, consulta previa con la misma identidad, y trabajo pendiente en la misma transacción de la aceptación. Construir también las cuatro respuestas de control, en la misma instancia y activadas por configuración, para que el flujo restante no cambie entre una ejecución y otra.

No se adopta una plataforma de mensajería durable como artefacto. No se diseña una saga. Polly queda limitado a fallos clasificados como transitorios y no sustituye la consulta ni la transacción local.

## Cierre de E7a

Hay ocho respuestas reales, agrupadas en cuatro contrastes, con costo, riesgo y sustento teórico. El 13 de octubre de 2026 el tesista confirmó tres decisiones:

1. La propuesta es la combinación de aceptación durable, efecto atómico, consulta previa con la misma identidad y trabajo pendiente en la misma transacción.
2. Los cuatro controles se construyen en la misma instancia y se activan por configuración, sin modificar el resto del flujo.
3. MassTransit, NServiceBus, CAP, Wolverine, Temporal, Dapr y Durable Task quedan fuera de la instancia. SQL Server, RabbitMQ y Polly son el vehículo; el código propio implementa las decisiones comparadas.

La decisión es coherente con la composición declarada en E2 y con los principios provisionales de E4. E7a queda cerrada.
