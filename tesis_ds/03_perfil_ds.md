# E3. Perfil de investigación Design Science

## Título provisional

**Arquitectura de referencia para integraciones asíncronas confiables con servicios externos: evaluación en transferencias financieras**

El tesista propuso ampliar el alcance a cualquier arquitectura monolítica conectada con otro servicio. Esa generalización se rechazó en su forma amplia porque el experimento no cubrirá integraciones síncronas, operaciones sin efectos persistentes ni la diversidad de fallos de todos los dominios. Se adoptó una abstracción intermedia: integraciones monolíticas que envían solicitudes asíncronas a un sistema externo, reciben confirmación inicial y obtienen posteriormente un resultado susceptible de duplicación. Las transferencias financieras permanecen como caso de evaluación y condición de contorno, no como límite absoluto de los principios.

El título fue seleccionado por el tesista porque declara la clase de problema y conserva visible el contexto concreto de evaluación.

## Planteamiento del problema

Los sistemas de origen que envían transferencias mediante una cámara operan con interacciones asíncronas: una solicitud obtiene una confirmación inmediata, mientras el resultado final llega posteriormente en otra interacción. En este contexto, un cierre programado de la cámara puede trasladar la indisponibilidad al usuario y obligarlo a repetir la operación; además, una respuesta satisfactoria duplicada puede aplicar más de una vez un efecto financiero si el sistema de origen no reconoce que corresponde a una transferencia ya procesada.

El problema de diseño consiste en construir una arquitectura que conserve solicitudes aceptadas durante cierres programados, las despache después de la reapertura sin nueva intervención del usuario y procese una sola vez resultados equivalentes. El problema de investigación —que en el formato institucional se registrará como **problema científico**— no es la ausencia de mecanismos: existen bandejas transaccionales, ejecución durable, reintentos, consulta e idempotencia. La revisión rápida no identificó una evaluación común que determine qué respuesta arquitectónica es necesaria para cada fallo seleccionado, qué garantía produce, qué costo introduce y bajo qué contrato externo conserva su validez.

## Pregunta de investigación

**¿Qué principios permiten seleccionar y combinar persistencia durable, identidad e idempotencia, y consulta o reintento para que sistemas monolíticos integrados con servicios externos mediante mensajería asíncrona conserven solicitudes y eviten efectos duplicados ante cierres temporales, pérdida de confirmación, resultados duplicados y fallos locales?**

Esta es una pregunta prescriptiva de nivel de clase: exige construir y evaluar el artefacto, incorpora utilidad y busca conocimiento reutilizable. En el formato institucional, la brecha que origina esta pregunta se registrará como **problema científico**.

Las subpreguntas son:

1. ¿Qué limitaciones presenta una arquitectura básica frente a los cuatro escenarios de fallo seleccionados?
2. ¿Qué componentes, estados, responsabilidades y contratos debe incluir la arquitectura de referencia para satisfacer los criterios definidos?
3. ¿Qué revela la evaluación formativa y qué cambios introduce en la segunda versión del artefacto?
4. ¿En qué medida la versión rediseñada supera la referencia básica respecto a pérdida de solicitudes, duplicación de efectos y recuperación?
5. ¿Qué principios de diseño y condiciones de contorno pueden extraerse de los resultados?

## Objeto de estudio

El objeto de estudio es una composición de modelo arquitectónico, principios de diseño e instancia experimental mínima. En Design Science, el objeto no es la transferencia bancaria ni el monolito histórico, sino el artefacto propuesto y evaluado.

## Objetivos

### Objetivo general

Diseñar y evaluar una arquitectura de referencia para sistemas monolíticos integrados con servicios externos mediante mensajería asíncrona, con el fin de identificar principios para seleccionar y combinar mecanismos de persistencia, idempotencia y recuperación según el fallo, la garantía requerida y las condiciones del servicio externo.

### Objetivos específicos

1. Caracterizar el flujo de integración, la arquitectura básica y los cuatro escenarios de fallo que representan la clase de problema.
2. Derivar de la base de conocimiento y del diagnóstico los requisitos, criterios de éxito y alternativas de solución antes de construir.
3. Diseñar e implementar variantes experimentales mínimas en .NET Core que permitan aislar las respuestas arquitectónicas comparadas en cada escenario.
4. Ejecutar una evaluación formativa, documentar sus hallazgos y rediseñar el artefacto.
5. Comparar ambas arquitecturas mediante una evaluación sumativa controlada y articular los principios de diseño y sus condiciones de contorno.

## Tipología del artefacto

El artefacto es una composición de tres tipos:

- **Modelo:** estados, componentes, responsabilidades, contratos y relaciones de la arquitectura de referencia.
- **Instanciación:** implementación mínima en .NET Core de una referencia básica y de la arquitectura propuesta.
- **Principios de diseño:** conocimiento prescriptivo derivado de la construcción, el rediseño y la comparación experimental.

El modelo contiene el conocimiento representacional; la instanciación permite generar evidencia; los principios constituyen la contribución de nivel de maestría.

## Campo de acción

Integraciones en las que un sistema monolítico envía a un servicio externo una solicitud que modifica estado, recibe una confirmación inicial y obtiene posteriormente un resultado asíncrono que puede repetirse. La evaluación se realizará mediante transferencias financieras salientes simuladas en un laboratorio .NET Core con datos sintéticos durante 2026. La arquitectura lógica será independiente del entorno de despliegue; la única instancia evaluada será local y se añadirá un mapeo ilustrativo, no evaluado, hacia servicios de AWS. Se excluyen integraciones síncronas, operaciones de solo lectura, acreditaciones entrantes, sobrecarga de la red interna y evaluación criptográfica.

## Núcleo de la contribución

El tesista formuló y explicó la siguiente contribución, ajustada solo para retirar calificativos no evaluables y precisar la propiedad de consistencia:

> Esta investigación aportará principios validados para determinar en qué fallos deben utilizarse persistencia durable, identidad e idempotencia, y consulta o reintento, cómo deben combinarse, qué garantía producen y bajo qué condiciones externas resultan aplicables.

El tesista explica «procesar una sola vez» como no ejecutar una segunda operación de negocio ante un resultado ya recibido. Reconoce que los principios no aplican a aplicaciones sin integración externa. La condición de contorno se amplía metodológicamente: tampoco se reclaman para integraciones síncronas, operaciones de solo lectura ni protocolos sin resultado posterior.

## Modelo preliminar de fallos

La mensajería asíncrona introduce fallos parciales que no aparecen como una caída total del sistema. La literatura sobre sistemas distribuidos, inyección de fallos e idempotencia respalda examinar pérdidas, duplicaciones y fallos entre cambios de estado (Ding et al., 2023; Kleppmann & Riccomini, 2026; Meiklejohn et al., 2021). Para este estudio se distinguen:

- **Cierre remoto explícito:** la cámara informa que no aceptó la solicitud durante una ventana programada.
- **Confirmación inicial perdida:** la solicitud puede haber sido aceptada, pero el sistema de origen no recibe la confirmación y no sabe si debe reenviar.
- **Resultado final duplicado:** la cámara entrega más de una vez el mismo resultado, por ejemplo porque la confirmación de recepción se perdió.
- **Fallo local entre pasos:** el sistema retiene fondos o acepta una solicitud, pero falla antes de registrar durablemente el siguiente paso.
- **Resultado tardío o fuera de orden:** una respuesta anterior puede llegar después de una transición posterior e intentar sobrescribirla.
- **Concurrencia sobre la misma identidad:** dos entregas equivalentes pueden procesarse simultáneamente y superar una comprobación no atómica.
- **Acumulación después de la recuperación:** muchas solicitudes pendientes pueden reanudarse simultáneamente y provocar una tormenta de reintentos.
- **Mensaje no procesable:** un mensaje válido para el transporte puede incumplir el contrato, tener una versión incompatible o contener datos inválidos.

Por el plazo, no todos se incorporarán a la evaluación. Se seleccionan cuatro escenarios que comparten el mismo flujo y pueden automatizarse:

1. **Cierre programado:** el servicio externo no acepta solicitudes durante cinco minutos; las solicitudes locales se conservan y se despachan después de la reapertura.
2. **Resultado final duplicado:** dos o más copias del mismo resultado no deben repetir el efecto de negocio.
3. **Confirmación inicial perdida:** el servicio externo pudo aceptar la solicitud, pero el origen no recibió la confirmación. El origen conserva la misma identidad, consulta el estado mediante una interfaz específica cuando esté disponible y solo reenvía la misma solicitud si el contrato remoto permite reconocer duplicados.
4. **Fallo local entre aceptación y despacho durable:** el proceso se interrumpe después de aceptar la solicitud o retener recursos, y debe recuperarla al reiniciarse sin pérdida ni creación de una nueva operación.

El cuarto escenario se utilizará primero en la evaluación formativa para descubrir debilidades y producir un rediseño documentado. Después se repetirá como prueba de regresión junto con los otros tres durante la evaluación sumativa. Esta selección es el límite del alcance; resultado fuera de orden, concurrencia extrema, tormenta de reintentos, mensajes no procesables y fallos criptográficos quedan fuera de la evaluación.

La garantía frente a la pérdida de confirmación tiene una condición de contorno: el sistema externo debe ofrecer consulta por identidad o reconocer la misma identidad en reenvíos. Sin una de esas capacidades, el sistema de origen no puede demostrar por sí solo que el efecto remoto ocurrió una sola vez.

## Fundamentación inicial en la base de conocimiento

La confiabilidad se descompone en propiedades que pueden especificarse y evaluarse mediante el modelo de calidad ISO/IEC 25010:2023. Los problemas de consistencia, mensajería y fallos parciales se fundamentarán en Kleppmann y Riccomini (2026); la idempotencia ante reintentos, en Ding et al. (2023); el comportamiento de patrones de resiliencia, en Mendonça et al. (2020); y la inyección de fallos a nivel de servicio, en Meiklejohn et al. (2021). Estas fuentes deberán modificar decisiones concretas del artefacto; una fuente que no cambie requisitos, componentes, contratos o evaluación no se conservará como fundamento.

## Diseño metodológico

La investigación seguirá las seis actividades del DSRM: identificación y motivación; objetivos de solución; diseño y desarrollo; demostración; evaluación; y comunicación (Peffers et al., 2007). Dentro de ese proceso se documentarán los ciclos de ingeniería e investigación de Wieringa (2014), de modo que cada versión registre su pregunta de diseño, evidencia, modificación y aprendizaje.

El punto de entrada es orientado al problema: existe una experiencia práctica previa, pero el artefacto se construirá desde cero y los criterios se fijarán antes de su implementación. No se reutilizarán código ni documentos bancarios.

## Estrategia de evaluación

La secuencia sigue FEDS (Venable et al., 2016):

1. **Evaluación artificial formativa:** se aplicará el fallo local entre aceptación y despacho durable sobre la primera versión. El resultado deberá producir al menos una decisión de rediseño documentada; si no revela ninguna debilidad, se revisará la capacidad discriminante del escenario.
2. **Rediseño y verificación interna:** se corregirá el artefacto y se repetirán las pruebas técnicas.
3. **Evaluación técnica sumativa por descomposición:** en cada escenario se compararán dos respuestas arquitectónicas reales: rechazo frente a aceptación durable; comprobación separada frente a efecto atómico; reenvío inmediato frente a consulta previa; y publicación directa frente a conservación transaccional.

No se declarará evaluación de campo. La evidencia demostrará comportamiento en condiciones controladas, no utilidad en operación bancaria real.

## Criterios comprometidos

- Cero dobles débitos o créditos ante resultados duplicados.
- Cero solicitudes aceptadas perdidas.
- Todas las operaciones en estado terminal o conciliable.
- Conservación de solicitudes aceptadas durante el cierre sin nueva intervención del usuario.
- Inicio del intento de envío dentro de los 10 segundos posteriores a la reapertura simulada.
- Espera de 10 segundos tras la pérdida de la confirmación inicial antes de consultar el estado.
- Sobrecarga de la aceptación local, sin fallos, menor o igual al 20% respecto de la variante directa.

## Tipo de contribución reclamada

La contribución reclamada es de nivel de clase: principios de selección y combinación con condiciones de contorno para integraciones asíncronas estructuralmente similares. Después de E5, la posición provisional en el marco de Gregor y Hevner (2013) es de mejora: los mecanismos son conocidos y la contribución pretendida es evidencia sobre su necesidad, interacción y límites. Si la evaluación solo confirma el comportamiento ya documentado de las herramientas, la contribución deberá reducirse y no podrá presentarse como conocimiento original. No se reclamará una teoría de diseño ni validez universal.

## Selección y muestra experimental

No habrá participantes humanos. Las unidades experimentales serán ejecuciones independientes de dos respuestas arquitectónicas por cada uno de los cuatro escenarios. Se planifican 30 repeticiones por alternativa y escenario, para un mínimo de 240 ejecuciones sumativas; cada ejecución reiniciará el estado y conservará registros automáticos. La cantidad definitiva se fijará en E8 antes de observar resultados.

La aplicación ejemplar utilizará cuentas, saldos, solicitudes y respuestas sintéticas. El simulador externo ofrecerá confirmación inicial, consulta por identidad, resultado posterior y reconocimiento de reenvíos con la misma identidad. Estas capacidades son condiciones de contorno declaradas, no propiedades atribuidas a todas las cámaras.

## Cronograma operativo

- **28–30 de septiembre:** cierre del perfil DS y protocolo inicial de búsqueda.
- **1–4 de octubre:** marco teórico, estado del arte y confirmación de la brecha.
- **5–6 de octubre:** diagnóstico, línea base conceptual, indicadores y criterios definitivos.
- **7–10 de octubre:** alternativas, requisitos, diseño y primera versión experimental.
- **11 de octubre:** evaluación formativa mediante fallo local.
- **12 de octubre:** rediseño y prueba de regresión inicial.
- **13–14 de octubre:** consolidación de perfil, ficha preliminar y preparación de predefensa.
- **15 de octubre:** predefensa.
- **16–23 de octubre:** construcción final, verificación interna y documentación reproducible.
- **24–29 de octubre:** evaluación sumativa automatizada.
- **30 de octubre–2 de noviembre:** análisis y articulación inicial de principios.
- **3–8 de noviembre:** redacción, amenazas a la validez y conclusiones.
- **9–11 de noviembre:** revisión final y correcciones.
- **12 de noviembre:** defensa prevista.

El cronograma no tiene holgura. Cualquier retraso obligará a reducir detalles de implementación, no a añadir escenarios ni a eliminar el rediseño.

## Plan de análisis

Para cada ejecución se extraerán: solicitudes aceptadas, solicitudes perdidas, identidades procesadas, efectos de negocio aplicados, estado final, consultas, reenvíos y tiempo hasta el despacho posterior a la reapertura. Los criterios binarios se informarán como conteos y proporciones observadas; los tiempos se resumirán mediante mediana, rango y percentil 95 cuando las repeticiones lo permitan.

La comparación se realizará por escenario entre dos respuestas arquitectónicas plausibles. No se inferirá generalización estadística a sistemas reales. La contribución se derivará trazando cada resultado hacia el mecanismo que lo produjo y formulando principios con objetivo, contexto, mecanismo y condición de contorno. Si un criterio falla en las ejecuciones reservadas, se informará el incumplimiento; no se redefinirá el criterio después de observar los datos.

Las amenazas principales previstas son: representación incompleta del constructo de confiabilidad; simplificación artificial del entorno; sesgo del investigador como constructor y evaluador; dependencia del contrato de consulta o deduplicación remota; y número limitado de escenarios. Sus mitigaciones serán trazabilidad, automatización, separación de configuraciones formativas y sumativas, publicación de código y registros, y delimitación explícita de las afirmaciones.

## Anexo previsto del artefacto

El anexo incluirá el modelo de estados, diagramas de componentes y secuencia, contratos de mensajes, decisiones y alternativas descartadas, código de ambas instancias, simulador externo, inyectores de fallos, datos sintéticos, instrucciones de ejecución, resultados brutos y registro de cambios entre versiones. Añadirá un mapeo ilustrativo de componentes hacia AWS, sin afirmar implementación ni validación en la nube. No contendrá nombres, código ni documentos de la entidad bancaria histórica.

## Cierre de E3

El perfil contiene título, ambos problemas, pregunta, objeto, objetivos, tipología, base inicial, método, evaluación, contribución, muestra, cronograma, análisis y anexo previsto. La oración de contribución fue formulada y explicada por el tesista. E3 quedó cerrada y fue actualizada después de E5 para incorporar la brecha corregida, la evaluación por descomposición y la posición provisional de mejora.
