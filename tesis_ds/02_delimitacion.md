# E2. Delimitación

## Punto de partida

El tema seleccionado es una arquitectura de referencia para transferencias salientes tolerantes a fallos transitorios. El estudio no intervendrá en una pasarela bancaria real ni afirmará mejorar el sistema histórico conocido por el tesista. Construirá desde cero dos instancias reducidas en .NET Core: una arquitectura básica de comparación y una arquitectura propuesta.

## Problema de diseño en elaboración

### Contexto preliminar

Servicios intermediarios de transferencias salientes que intercambian mensajes, por medio de una cámara de transferencias, con bancos u otras instituciones financieras que no comparten una transacción atómica. La interacción está confirmada como asíncrona a nivel de negocio: cada envío obtiene una confirmación inmediata y cierra su conexión, mientras el resultado llega posteriormente mediante una nueva interacción. El entorno de investigación será un laboratorio controlado con servicios simulados y datos sintéticos.

El diseño será neutral respecto al formato y protocolo de intercambio. La experiencia histórica con SOAP/XML sirve para comprender el flujo, pero no se afirmará una migración posterior a REST/JSON ni el uso de JWT porque el tesista no dispone de evidencia actual.

Como antecedente práctico, el tesista describe una arquitectura monolítica que concentraba la validación, la retención, el débito y la liberación de fondos. Un servicio web SOAP enviaba el mensaje inicial 1A a la cámara, recibía la confirmación 1B y cerraba la conexión. Más adelante, la cámara iniciaba otra interacción con el resultado final y el sistema de origen respondía con una confirmación. El gráfico aportado representa el retorno como 4A/4B, mientras el tesista recuerda 5A/5B para la variante implementada; la numeración no se utilizará como fundamento del artefacto. Este antecedente procede de memoria profesional y se abstraerá como una arquitectura genérica; no se presentará como documentación de una entidad concreta.

### Flujo funcional de referencia

1. Un cliente solicita transferir fondos a una cuenta de otra institución financiera.
2. El sistema de origen valida el estado de la cuenta y la disponibilidad de fondos.
3. El sistema prepara una solicitud con la información necesaria para la transferencia.
4. Los fondos quedan retenidos y la solicitud se envía a la cámara de transferencias.
5. La cámara procesa o encamina la solicitud de forma asíncrona.
6. El sistema de origen recibe posteriormente un mensaje con el resultado.
7. Ante un resultado satisfactorio, consolida el débito; ante un rechazo, libera la retención. Si la respuesta no llega, la operación permanece incierta y requiere recuperación o conciliación.

El sistema histórico era percibido por el tesista como un flujo binario —la transferencia llegaba y se debitaba, o no llegaba y se liberaba la retención— y no exponía un modelo explícito de estados. La arquitectura propuesta sí incorporará estados observables, porque la pérdida de respuesta introduce una tercera situación: el resultado es desconocido para el sistema de origen aunque la cámara o la institución receptora puedan haber procesado la solicitud.

Un rechazo durante la validación inicial no genera retención. Un rechazo final, recibido después de haber retenido fondos, debe liberar siempre los recursos. En la arquitectura propuesta, una solicitud aceptada durante el cierre se conservará localmente con fondos retenidos hasta su despacho automático o rechazo definitivo. La respuesta final llega en una interacción posterior e independiente de la confirmación inicial.

### Problema preliminar

La cámara presenta un cambio operativo de aproximadamente cinco minutos durante el cual no recibe transferencias y puede responder con un código de indisponibilidad. El sistema histórico consultaba una franja horaria configurada para evitar envíos durante ese periodo, pero, según el testimonio del tesista, algunas solicitudes atravesaban ese control. El usuario final veía entonces un mensaje de indisponibilidad y debía iniciar nuevamente la operación. Además, la cámara podía devolver más de una vez un mensaje satisfactorio para la misma transferencia, con riesgo de aplicar doble débito o doble crédito.

La pérdida de respuesta, la desconexión y el reenvío siguen siendo escenarios secundarios vinculados al resultado incierto. La sobrecarga de transferencias entrantes para acreditación corresponde a otro flujo, otros actores y otras métricas; queda explícitamente fuera del alcance.

### Solución conceptual preliminar

Una composición de modelo arquitectónico, principios de diseño e instancia experimental que preserve la identidad y trazabilidad de cada transferencia, impida el procesamiento repetido de respuestas equivalentes, difiera el envío durante cierres conocidos y reenvíe de forma controlada después de una respuesta transitoria de indisponibilidad.

Esta descripción expresa capacidades, no decisiones técnicas definitivas. El uso de una cola, un planificador, una bandeja transaccional u otros mecanismos se comparará en E7 antes de seleccionar la solución.

El comportamiento esperado es el siguiente: el sistema de origen acepta y conserva de forma durable la solicitud durante el cierre, la identifica como pendiente de envío y evita remitirla a la cámara. Después de la reapertura, la envía automáticamente con la misma identidad, sin exigir que el usuario repita la operación. Si recibe respuestas satisfactorias duplicadas, aplica el efecto local una sola vez. No se presupone que la cámara o la institución receptora utilice una cola de mensajes; ambas se representan como sistemas externos.

Una respuesta explícita de cámara cerrada puede tratarse como reintentable solo si su semántica garantiza que la solicitud no fue aceptada. Una pérdida de respuesta es diferente: deja un resultado incierto y no autoriza un reenvío ciego, salvo que el protocolo remoto reconozca una identidad idempotente estable. De otro modo, primero debe consultarse o conciliarse el resultado.

### Criterios de éxito pendientes

Los criterios se definirán antes de construir. Las propiedades inicialmente propuestas por el tesista son ausencia de duplicidad, disponibilidad continua, velocidad de respuesta y seguridad. Requieren los siguientes ajustes:

- **Integridad transaccional:** no deben producirse dobles débitos ni dobles créditos ante reintentos o mensajes duplicados.
- **Conservación y trazabilidad:** toda solicitud aceptada debe conservar identidad, historial y resultado observable.
- **Recuperabilidad:** una operación incierta debe alcanzar un estado terminal o quedar identificada para conciliación después de cesar el fallo.
- **Continuidad ante indisponibilidad remota:** la cámara realiza un cambio operativo durante el cual puede responder que el servicio no está disponible por aproximadamente cinco minutos. No se intentará demostrar disponibilidad 24/7; se evaluará si la arquitectura reconoce esa respuesta como transitoria, conserva la solicitud y puede procesarla después de la recuperación sin pérdida ni duplicación.
- **Despacho posterior a la reapertura:** cada solicitud pendiente debe iniciar un intento de envío dentro de los 10 segundos posteriores a que el simulador de la cámara vuelva a estar disponible. Este umbral aplica al laboratorio y no implica que la institución receptora concluya sus validaciones en ese tiempo.
- **Rendimiento temporal:** se distinguirá el tiempo de aceptación local del tiempo hasta el resultado final, porque la comunicación es asíncrona.
- **Seguridad:** la experiencia histórica empleaba firma digital con certificados sobre mensajes XML para verificar origen e integridad. En una interfaz HTTP con JSON existen mecanismos normalizados equivalentes a nivel de mensaje, como JWS (Jones et al., 2015) y HTTP Message Signatures (Backman et al., 2024). Su selección y evaluación criptográfica quedan fuera del alcance; el laboratorio asumirá canales autenticados y mensajes íntegros.

Todavía no se han fijado umbrales ni número de ejecuciones.

## Problema de investigación

El problema de investigación —que en el formato institucional se registrará como **problema científico**— se formula provisionalmente como la siguiente brecha: la literatura ofrece patrones generales de recuperación, análisis de reintentos y disyuntores, y métodos para verificar idempotencia en sistemas distribuidos (Amiri et al., 2023; Ding et al., 2023; Mendonça et al., 2020), pero la búsqueda exploratoria todavía no encontró evidencia empírica específica sobre qué combinación de capacidades arquitectónicas evita efectos duplicados y conserva solicitudes en transferencias financieras asíncronas sometidas a cierres programados y respuestas satisfactorias duplicadas. Este estudio pretende generar evidencia experimental sobre esa combinación en un entorno .NET Core.

La brecha es provisional. No equivale a afirmar que no existe una solución; deberá confirmarse mediante el estado del arte antes de reclamar novedad.

## Objeto de estudio preliminar

En Design Science, el objeto de estudio no es el proceso bancario ni las transferencias observadas históricamente. Es el artefacto propuesto: una composición de modelo arquitectónico, principios de diseño e instancia experimental para transferencias salientes sometidas a fallos transitorios.

## Campo de acción preliminar

Integraciones asíncronas de transferencias salientes entre bancos u otras instituciones financieras conectadas mediante una cámara, evaluadas en un entorno experimental .NET Core durante 2026. El foco primario comprende respuestas satisfactorias duplicadas y cierres programados de cinco minutos; la pérdida de respuesta y la desconexión se consideran únicamente cuando generan reenvío o resultado incierto.

## Restricción de alcance

La evaluación será artificial y experimental. Por ello, cualquier principio de diseño resultante quedará limitado a sistemas con supuestos equivalentes a los del laboratorio; no se reclamará validez para toda pasarela de pagos ni para operación bancaria en producción.

El tiempo de validación y filtrado dentro de cada institución receptora no está bajo control de la arquitectura propuesta. Las mediciones temporales se limitarán a la sobrecarga local, el reconocimiento de la solicitud y la recuperación de operaciones pendientes después de restablecerse el servicio simulado.

## Formulación consolidada de E2

### Problema de diseño

En sistemas de origen que envían transferencias a otras instituciones financieras mediante una cámara y un protocolo asíncrono de solicitud, confirmación y resultado posterior, los cierres programados de cinco minutos pueden exponer mensajes de indisponibilidad al usuario y obligarlo a repetir la operación. A su vez, la recepción duplicada de resultados satisfactorios puede aplicar más de una vez el débito o el crédito si el procesamiento local no distingue la identidad de la transferencia. Se requiere un artefacto arquitectónico que acepte y conserve solicitudes durante el cierre, difiera y recupere su envío, y procese una sola vez resultados equivalentes.

### Problema de investigación

La literatura ofrece patrones generales de recuperación, análisis de reintentos y métodos para verificar idempotencia en sistemas distribuidos, pero la búsqueda exploratoria no identificó evidencia específica sobre qué combinación de capacidades arquitectónicas evita efectos duplicados y conserva solicitudes en transferencias financieras asíncronas sometidas a cierres programados y resultados satisfactorios duplicados. Este problema de investigación se registrará como **problema científico** en el formato institucional y se revisará cuando el estado del arte determine la novedad relativa.

### Objeto de estudio

El objeto de estudio es una composición de modelo arquitectónico, principios de diseño e instancia experimental mínima para el procesamiento confiable de transferencias salientes asíncronas. Esta resignificación es propia de Design Science: el objeto no es la transferencia bancaria ni el monolito histórico, sino el artefacto que se construirá y evaluará.

### Campo de acción

Integraciones de transferencias salientes entre bancos u otras instituciones financieras conectadas mediante una cámara, bajo protocolos asíncronos con confirmación inicial y resultado posterior, evaluadas en un laboratorio .NET Core durante 2026. Se incluyen cierres programados, respuestas explícitas de indisponibilidad y resultados duplicados; se excluyen acreditaciones entrantes, sobrecarga interna y evaluación criptográfica.

### Propuesta

Una arquitectura de referencia compuesta por un modelo de estados y responsabilidades, principios arquitectónicos y una instancia experimental. La instancia comparará el comportamiento de una referencia básica con el de la propuesta frente a los mismos escenarios de fallo.

### Criterios de éxito

- Cero dobles débitos o créditos ante resultados duplicados.
- Cero solicitudes aceptadas perdidas.
- Todas las operaciones deben alcanzar un estado terminal o conciliable.
- Las solicitudes aceptadas durante el cierre deben conservarse sin intervención posterior del usuario.
- Cada solicitud pendiente debe iniciar un intento de envío dentro de los 10 segundos posteriores a la reapertura simulada.

## Cierre de E2

Los cuatro componentes del problema de diseño están delimitados; el problema de investigación está formulado como brecha provisional; el objeto de estudio declara el artefacto y su tipología; y el campo de acción está acotado. E2 queda cerrada, sujeto a que E5 confirme o refute la novedad relativa planteada.
