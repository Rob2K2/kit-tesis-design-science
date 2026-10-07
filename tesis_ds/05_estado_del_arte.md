# E5. Estado del arte

## Propósito y tipo de revisión

Esta etapa examina arquitecturas, patrones, bibliotecas y herramientas que abordan la conservación de solicitudes, la entrega repetida, los reintentos y la recuperación en integraciones asíncronas. Debido al plazo, se realizará una **revisión rápida estructurada**; no se presentará como revisión sistemática exhaustiva.

## Pregunta de revisión

**¿Qué soluciones existentes permiten a una aplicación .NET integrada asíncronamente con un servicio externo conservar solicitudes aceptadas, recuperar publicaciones inciertas y evitar la repetición de efectos de negocio, y qué limitaciones mantienen frente a los cuatro escenarios del estudio?**

## Subpreguntas

1. ¿Qué garantías ofrecen frente al fallo entre cambio de estado local y publicación?
2. ¿Cómo detectan y tratan solicitudes o resultados duplicados?
3. ¿Qué hacen cuando se pierde una confirmación y el resultado remoto es incierto?
4. ¿Permiten diferir envíos durante indisponibilidades programadas y reanudarlos de forma controlada?
5. ¿Qué dependencias, costos, supuestos y límites de transferencia introducen?
6. ¿Cómo fueron evaluadas y qué evidencia respalda sus garantías?

## Fuentes y búsqueda

Se consultarán publicaciones primarias y documentación oficial de soluciones. Las cadenas iniciales son:

- `"transactional outbox" .NET SQL Server RabbitMQ`
- `"idempotent consumer" .NET RabbitMQ`
- `MassTransit transactional outbox inbox duplicate detection`
- `NServiceBus outbox exactly once processing`
- `CAP distributed transaction outbox RabbitMQ SQL Server`
- `Wolverine durable messaging inbox outbox`
- `RabbitMQ publisher confirms consumer acknowledgements duplicate`
- `Polly retry idempotency transient fault`

## Criterios de selección

### Inclusión

- Solución aplicable a .NET o principio implementable en la instancia prevista.
- Documentación oficial, código abierto verificable o publicación primaria.
- Descripción explícita de garantías, persistencia, duplicados, reintentos o recuperación.
- Evidencia suficiente para reconstruir supuestos y limitaciones.

### Exclusión

- Tutoriales sin autoría o procedencia verificable.
- Soluciones centradas exclusivamente en seguridad, interfaz o procesamiento masivo.
- Afirmaciones de «exactamente una vez» que no definan la frontera de la garantía.
- Productos sin documentación técnica accesible sobre el mecanismo.

## Criterios de comparación

Cada solución se contrastará contra:

1. conservación durable antes de confirmar aceptación;
2. atomicidad entre estado local y trabajo pendiente;
3. identidad estable y deduplicación del efecto;
4. cierre programado y despacho posterior;
5. confirmación inicial perdida y consulta o reenvío seguro;
6. recuperación después de fallo local;
7. soporte para SQL Server y RabbitMQ;
8. costo, complejidad, licencia y dependencia de proveedor;
9. evidencia de evaluación;
10. transferibilidad del mecanismo más allá de la herramienta.

## Soluciones examinadas

### RabbitMQ y políticas de resiliencia implementadas por la aplicación

RabbitMQ ofrece confirmaciones al publicador, reconocimientos del consumidor, persistencia y reentrega, pero su garantía de entrega al menos una vez permite duplicados cuando se pierde una confirmación o falla un consumidor (RabbitMQ, s. f.-a, s. f.-b). Polly y las bibliotecas vigentes de resiliencia para .NET permiten reintento, espera, disyuntor y tiempo límite, pero no hacen atómica una modificación en SQL Server con la publicación ni convierten una operación externa en idempotente (Microsoft, s. f.-b). Esta alternativa tiene dependencia tecnológica baja y control alto, pero exige que la aplicación construya persistencia del trabajo pendiente, deduplicación, estados y conciliación.

### MassTransit

MassTransit ofrece bandejas de entrada y salida transaccionales con Entity Framework, SQL Server y RabbitMQ. Su bandeja de entrada usa `MessageId` y una ventana configurable de detección; la bandeja de salida persiste mensajes y los entrega después de confirmar el trabajo del consumidor (MassTransit, s. f.-a, s. f.-b). Resuelve gran parte del fallo local y de la reentrega dentro del ecosistema de mensajería. No determina el estado de una operación en un servicio externo ajeno al bus, no define la semántica de un cierre de cámara y su deduplicación depende de identidad, almacenamiento y ventana de retención configurados.

### NServiceBus

NServiceBus Outbox almacena en una misma transacción los cambios de negocio y los mensajes salientes, y deduplica por identidad del mensaje con persistencia SQL. La documentación incluye una aplicación con RabbitMQ y advierte que los efectos no transaccionales todavía pueden ejecutarse más de una vez; las garantías exigen que los datos de negocio participen en la misma conexión y transacción de la bandeja de salida (Particular Software, s. f.-a, s. f.-b). Es una solución madura para atomicidad local y duplicados, pero añade una plataforma comercial y no resuelve por sí sola consulta de estado, cierre remoto ni unicidad de efectos en un sistema externo.

### CAP

CAP persiste el cambio local y la intención de publicación en una transacción de SQL Server y entrega posteriormente mediante RabbitMQ. Incluye reintentos configurables, pero declara entrega al menos una vez y exige que el consumidor implemente idempotencia; su documentación ejemplifica el riesgo de repetir débitos y señala límites temporales de sus registros internos (DotNetCore.CAP, s. f.-a, s. f.-b). Es una alternativa ligera para la atomicidad local, pero deja la deduplicación de negocio y la conciliación remota a la aplicación.

### Wolverine

Wolverine ofrece bandejas durables de entrada y salida con SQL Server, middleware transaccional de Entity Framework y transporte RabbitMQ. Su bandeja de entrada deduplica por clave primaria y conserva temporalmente mensajes procesados; la salida implementa almacenamiento y reenvío después de la transacción (Wolverine, s. f.-a, s. f.-b). Reduce código de infraestructura y cubre el fallo local, aunque la ventana de conservación, el contrato de identidad y la recuperación del resultado externo continúan siendo decisiones del sistema.

### Temporal, Dapr y Durable Task

Los motores de ejecución durable guardan historial y reanudan actividades después de fallos. Temporal reintenta actividades de forma durable, pero exige efectos idempotentes porque una actividad puede completarse y ejecutarse otra vez si su resultado no fue registrado (Temporal Technologies, s. f.). Dapr combina flujo durable y bandeja transaccional, aunque sus actividades también se ejecutan al menos una vez y la superposición de políticas puede producir más reintentos que los previstos (Dapr, s. f.-a, s. f.-b). Durable Task aplica la misma separación entre orquestación durable y actividades idempotentes (Microsoft, s. f.-a). Estas plataformas resuelven recuperación del flujo, pero no pueden garantizar unilateralmente la unicidad de un efecto en un servicio remoto.

### Contratos de proveedores de pago

PayPal exige una identidad estable para reintentar solicitudes con respuesta incierta y devuelve el estado actual asociado a esa identidad, evitando ejecutar nuevamente una operación ya completada (PayPal, s. f.). SIBS documenta notificaciones asíncronas con entrega al menos una vez, duplicación, retraso y desorden; prescribe procesamiento idempotente, estados finales monotónicos y consulta de estado para verificación, recuperación y conciliación (SIBS, s. f.-a, s. f.-b). Esta evidencia contradice la brecha inicial si se formula como ausencia de una combinación entre identidad, consulta y deduplicación: esa combinación ya existe como práctica documentada en el dominio.

## Síntesis comparativa

Las soluciones examinadas convergen en cuatro mecanismos: registro durable, identidad estable, procesamiento idempotente y reintento controlado. MassTransit, NServiceBus, CAP, Wolverine y Dapr resuelven de distintas formas la atomicidad local entre datos y trabajo pendiente. Temporal, Dapr y Durable Task resuelven la continuidad del flujo. PayPal y SIBS muestran que la incertidumbre remota requiere cooperación contractual mediante identidad y consulta; ninguna biblioteca local puede sustituir esa capacidad.

Ninguna herramienta aislada cubre automáticamente los cuatro escenarios con la semántica del estudio. Sin embargo, esto no constituye por sí solo novedad científica: es normal que una arquitectura integre mecanismos existentes. La pregunta relevante pasa a ser qué mecanismos son necesarios para cada clase de fallo, qué garantía produce cada combinación y qué condición externa limita esa garantía.

## Insuficiencias relativas

1. Las bandejas transaccionales resuelven atomicidad local, no el resultado incierto de una operación externa ya aceptada.
2. Las ventanas de deduplicación pueden expirar; la identidad técnica del mensaje no siempre coincide con la identidad estable de la operación de negocio.
3. Los motores durables reejecutan actividades y trasladan la obligación de idempotencia al efecto externo.
4. El reintento automático no distingue por sí mismo rechazo definitivo, cierre conocido y confirmación perdida.
5. La consulta de estado depende del contrato del proveedor; sin consulta o idempotencia remota no existe garantía extremo a extremo.
6. La documentación de producto demuestra capacidades, pero ofrece poca evidencia comparativa sobre qué combinación mínima resuelve cada escenario en un monolito existente.
7. Las soluciones suelen evaluar rendimiento o funcionamiento propio, no trazabilidad problema → mecanismo → garantía → condición de contorno.

## Corrección de la brecha

La brecha inicial afirmaba que no se había identificado evidencia sobre la combinación de conservación, reintento, consulta e idempotencia en transferencias asíncronas. Esa formulación es demasiado amplia y queda refutada por la documentación de proveedores de pago y plataformas de mensajería.

La brecha corregida es más estrecha:

> La literatura y las plataformas ofrecen mecanismos consolidados de persistencia, reintento, ejecución durable, consulta e idempotencia, pero la revisión rápida no identificó una evaluación controlada que aísle qué mecanismos y contratos son necesarios para cada uno de los cuatro fallos seleccionados en una integración monolítica asíncrona, ni que articule las garantías resultantes y sus condiciones de contorno mediante una aplicación financiera reproducible en .NET.

Esta brecha solo será defendible si la evaluación no se limita a comparar una arquitectura deliberadamente defectuosa con otra completa. Se requiere una **evaluación por descomposición**: habilitar o retirar de manera controlada persistencia durable, identidad/deduplicación y consulta/reintento, para observar qué propiedad pierde cada variante. Así, la contribución no será inventar los mecanismos, sino producir evidencia sobre su necesidad, interacción y frontera de garantía.

## Criterios de superioridad derivados

La propuesta deberá demostrar, frente a variantes controladas:

- qué mecanismo evita la pérdida después de aceptar;
- qué mecanismo evita repetir el efecto ante resultados duplicados;
- qué combinación resuelve una confirmación inicial perdida;
- qué condición externa impide garantizar un único efecto remoto;
- qué costo temporal introduce cada combinación;
- qué principios permanecen independientes de MassTransit, NServiceBus, CAP, Wolverine o un motor durable.

## Riesgo de contribución

Si se implementa únicamente una bandeja transaccional con deduplicación y se demuestra que funciona, la tesis quedará en nivel de instancia y reproducirá soluciones existentes. Para mantener nivel de maestría, la evaluación debe aislar mecanismos, documentar el rediseño formativo y derivar principios con condiciones de contorno. La ausencia de evaluación de campo sigue limitando la transferibilidad.

## Estado de la puerta E5

La revisión demuestra por qué ninguna solución local garantiza por sí sola el efecto remoto y también demuestra que los mecanismos principales ya son conocidos. El tesista acepta que la contribución no será inventar esos mecanismos y acepta evaluarlos por descomposición. Para evitar una comparación trivial entre una solución completa y otra deliberadamente defectuosa, cada escenario contrastará dos respuestas arquitectónicas reales:

1. cierre programado: rechazo al usuario frente a aceptación durable y despacho diferido;
2. resultado duplicado: comprobación separada del efecto frente a decisión y efecto atómicos por identidad;
3. confirmación perdida: reenvío inmediato frente a consulta previa y reenvío con identidad estable;
4. fallo local: publicación directa posterior al cambio local frente a conservación transaccional del trabajo pendiente.

Con 30 repeticiones por alternativa y escenario se mantienen las 240 ejecuciones previstas. El tesista formula el conocimiento buscado como determinar «en qué casos debe utilizarse [cada mecanismo] y de qué forma», y reconoce que no inventará los mecanismos. Esta formulación se consolida como principios de selección y combinación: fallo → alternativas → mecanismo necesario → garantía → costo → condición de contorno.

E5 queda cerrada. La brecha inicial fue corregida con evidencia, las soluciones existentes fueron comparadas y la contribución se calibró al conocimiento que la evaluación puede producir. La revisión es rápida y no exhaustiva; esa limitación deberá mantenerse en la tesis.
