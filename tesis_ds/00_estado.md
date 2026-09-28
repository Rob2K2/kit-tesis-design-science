# Estado de la tesis

## Encuadre

- **Programa:** Maestría en Ingeniería de Software.
- **Paradigma metodológico:** Design Science.
- **Tutor:** Luis Roberto Pérez Rios, Ph.D.
- **Tesista:** Roberto Carlos Merino Almaraz.
- **Fecha de inicio:** 23 de septiembre de 2026.
- **Predefensa planificada:** 15 de octubre de 2026.
- **Defensa planificada:** 12 de noviembre de 2026.
- **Etapa actual:** E4. Marco teórico.

## Etapas completadas

- E0. Encuadre, cerrada el 23 de septiembre de 2026.
- E1. Áreas y temas, cerrada el 27 de septiembre de 2026.
- E2. Delimitación, cerrada el 27 de septiembre de 2026.
- E3. Perfil de investigación DS, cerrada el 28 de septiembre de 2026.

## Decisiones tomadas

- La contribución esperada será de nivel de maestría: principios de diseño validados y transferibles analíticamente a una clase de problemas.
- El artefacto será el vehículo para generar conocimiento, no la contribución final por sí solo.
- Se exigirá al menos un ciclo de evaluación formativa con rediseño documentado y un ciclo de evaluación sumativa.
- Los criterios de éxito se fijarán antes de construir el artefacto.
- Los documentos de trabajo se producirán en Markdown y las citas y referencias seguirán APA 7.
- El tesista empieza sin tema, documento ni artefacto previamente definidos, pero informa que tiene ideas iniciales que se examinarán en E1.
- La predefensa se planifica para el 15 de octubre de 2026.
- La defensa se planifica para el 12 de noviembre de 2026.
- El área de mayor experiencia es el desarrollo de aplicaciones de escritorio, con especialidad en servicios de servidor.
- El tesista cuenta con 15 años de experiencia en banca, instituciones educativas y, actualmente, una factoría de software.
- La disponibilidad se concentra en horario nocturno y alcanza aproximadamente 15 horas semanales.
- La idea inicial es proponer un artefacto arquitectónico para una pasarela de pagos. La expresión «arquitectura robusta» aún no constituye una propiedad operacional ni una tipología de artefacto suficientemente delimitada.
- El tesista domina .NET Core y tiene experiencia directa en la implementación de transferencias interbancarias.
- El contexto que originó la idea ya no está disponible: no existe acceso actual al sistema bancario, a su código, a sus datos ni a su operación.
- La evidencia inicial consiste en el testimonio del tesista sobre duplicidades, interrupciones de red, retención de fondos y periodos breves de indisponibilidad.
- La comparación arquitectónica se realizará mediante una instancia experimental mínima; no se afirmará que mejora el sistema bancario anterior.
- El tesista rectifica que no puede asegurar evaluadores independientes y que solo cuenta con el tutor presencial.
- El alcance experimental aceptado se limita a transferencias salientes y a cuatro escenarios: desconexión, pérdida de respuesta, duplicación e indisponibilidad temporal.
- Se identificaron tres temas candidatos: arquitectura de referencia tolerante a fallos, modelo de estados y recuperación, y método de pruebas por inyección de fallos.
- El tesista selecciona el Tema 1: arquitectura de referencia para transferencias salientes tolerantes a fallos transitorios.
- Ante la ausencia de evaluadores independientes, se realizará una evaluación formativa técnica con rediseño y una evaluación sumativa experimental con escenarios reservados, ejecuciones repetidas y registros automáticos. El tutor revisará la coherencia metodológica, pero no será presentado como evaluador independiente.
- El documento bancario histórico queda excluido. La arquitectura de comparación será genérica y se construirá desde cero para evitar riesgos de confidencialidad o propiedad intelectual.
- El flujo delimitado comprende solicitud, validación, retención de fondos, envío asíncrono mediante una cámara, recepción posterior del resultado y consolidación del débito o liberación de la retención.
- La arquitectura será neutral respecto a SOAP/XML, REST/JSON y mecanismos de autenticación no verificados.
- Los atributos preliminares son integridad transaccional, trazabilidad, recuperabilidad, continuidad ante indisponibilidad remota y rendimiento temporal. La disponibilidad 24/7 no se reclamará a partir de un experimento breve.
- La indisponibilidad prioritaria corresponde a un cierre programado de la cámara durante un cambio operativo de aproximadamente cinco minutos; algunas solicitudes atravesaban el control horario y requerían reenvío.
- La arquitectura propuesta incorporará estados transaccionales observables aunque el sistema histórico no los expusiera explícitamente.
- La seguridad criptográfica queda fuera de la evaluación. Se asumirá un canal autenticado y mensajes íntegros; JWS y HTTP Message Signatures se registran únicamente como mecanismos normalizados posibles para interfaces HTTP/JSON.
- El segundo problema primario son respuestas satisfactorias duplicadas para una misma transferencia, con riesgo de doble débito o crédito.
- La sobrecarga de transferencias entrantes y la acreditación lenta quedan fuera de alcance porque corresponden a otro flujo.
- Los criterios aceptados incluyen cero dobles efectos, cero solicitudes aceptadas perdidas y todas las operaciones en estado terminal o conciliable.
- Durante el cierre, el usuario final recibía un mensaje de indisponibilidad y debía iniciar nuevamente la operación.
- La propuesta aceptará y conservará la solicitud localmente, evitará enviarla durante el cierre y realizará un intento automático dentro de los 10 segundos posteriores a la reapertura simulada.
- No se asumirá una cola de mensajes en la cámara o en la institución receptora.
- El antecedente práctico era un monolito que concentraba validación, retención, débito y liberación, integrado con la cámara mediante un servicio web SOAP.
- Un rechazo inicial no retenía fondos; un rechazo final debía liberar la retención. Falta aclarar en qué momento ocurría la respuesta de cámara cerrada.
- El protocolo histórico queda confirmado como asíncrono: 1A obtenía 1B y cerraba la conexión; el resultado final llegaba posteriormente mediante otra interacción con su propia confirmación.
- La numeración específica de los mensajes se conservará solo como antecedente y no condicionará el artefacto.
- E2 consolidó el problema de diseño, la brecha provisional, el objeto de estudio como artefacto compuesto, el campo de acción, la propuesta y cinco criterios de éxito.
- En E3, el tesista rechazó el primer título provisional.
- La primera formulación de contribución —manejo de arquitectura en sistemas monolíticos o distribuidos— no supera la puerta de calidad porque nombra un área amplia y no el conocimiento original.
- La utilidad transferible identificada por el tesista consiste en que otros arquitectos reconozcan falencias de su arquitectura inicial y determinen qué incorporar para tolerar fallos. Esta idea debe traducirse a principios específicos sobre cierres programados, conservación de solicitudes y resultados duplicados.
- El título provisional seleccionado es «Arquitectura de referencia para el procesamiento confiable de transferencias financieras asíncronas».
- El núcleo de la contribución seleccionado es producir principios para conservar solicitudes y procesar una sola vez resultados duplicados.
- El tesista propuso generalizar desde banca hacia arquitecturas monolíticas conectadas con servicios externos. Se rechazó la generalización a todo monolito y se adoptó una clase intermedia: integraciones asíncronas con confirmación inicial, resultado posterior, indisponibilidad temporal y posible duplicación.
- El título vigente es «Arquitectura de referencia para integraciones asíncronas confiables con servicios externos: evaluación en transferencias financieras».
- La oración de contribución fue reformulada y explicada por el tesista: los principios permitirán conservar solicitudes aceptadas durante indisponibilidades temporales y evitar aplicar más de una vez los efectos de resultados duplicados.
- La puerta de contribución de E3 está superada.
- La evaluación queda limitada a cuatro escenarios del mismo flujo: cierre programado, resultado final duplicado, confirmación inicial perdida y fallo local entre aceptación y despacho durable.
- Ante la pérdida de confirmación inicial, se conservará la misma identidad; se consultará el estado cuando exista una interfaz para ello y solo se reenviará bajo un contrato remoto que reconozca duplicados.
- El fallo local se aplicará primero como evaluación formativa, provocará rediseño y se repetirá como regresión en la evaluación sumativa.
- La consulta remota por identidad o el reconocimiento de la misma identidad en reenvíos es una condición de contorno; sin ella, el origen no puede garantizar por sí solo un único efecto remoto.
- El perfil DS quedó consolidado con pregunta principal, cinco subpreguntas, objetivo general, cinco objetivos específicos, tipología compuesta, DSRM, estrategia FEDS, muestra experimental inicial de 240 ejecuciones, cronograma y plan de análisis.
- La evaluación formativa y el rediseño se planifican antes de la predefensa; la evaluación sumativa se planifica entre el 24 y el 29 de octubre.

## Decisiones pendientes

- Construir el marco teórico que justifique cada decisión significativa del artefacto.
- Fijar la pregunta y el protocolo de búsqueda teórica de E4.
- Determinar cuántas fuentes recientes verificadas son suficientes y comprobar la prueba de eliminación.

## Próxima etapa

Cerrar E4 con una base de conocimiento que cambie decisiones concretas del diseño, no con una descripción general del dominio.
