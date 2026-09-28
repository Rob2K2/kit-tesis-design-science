# E1. Áreas y temas

## Perfil profesional relevante

Roberto Carlos Merino Almaraz cuenta con 15 años de experiencia profesional. Su área de mayor experiencia es el desarrollo de aplicaciones de escritorio y su especialidad declarada es el desarrollo de servicios de servidor. Ha trabajado en banca, instituciones educativas y actualmente se desempeña en una factoría de software.

Su disponibilidad para la tesis se concentra en horario nocturno y alcanza aproximadamente 15 horas semanales. Domina .NET Core y cuenta con experiencia directa en la implementación de un sistema de transferencias interbancarias que enviaba y recibía dinero de otras entidades financieras.

## Idea inicial

El tesista propone inicialmente un artefacto arquitectónico orientado a una pasarela de pagos. La idea surge de su experiencia en un sistema bancario de transferencias interbancarias que, según su testimonio, presentaba los siguientes problemas:

- duplicidad de operaciones;
- interrupciones de red que dejaban fondos retenidos o pignorados debido al tratamiento insuficiente de reintentos;
- respuestas temporales de no disponibilidad durante un cambio de ciclo de aproximadamente un minuto.

El tesista plantea comparar la arquitectura utilizada entonces con una arquitectura alternativa más tolerante a fallos y considera una cola de mensajes como posible mecanismo. Esta solución técnica todavía es una hipótesis: antes de adoptarla deben considerarse los requisitos de idempotencia, consistencia, confirmación de operaciones y recuperación ante resultados inciertos.

La formulación «modelo de arquitectura robusta» continúa siendo preliminar por tres razones:

1. «Robusta» agrupa posibles atributos diferentes —por ejemplo, disponibilidad, tolerancia a fallos, recuperabilidad, seguridad, integridad transaccional o capacidad de evolución— y todavía no tiene una definición operacional.
2. Los problemas proceden del recuerdo profesional del tesista; todavía no existen registros, mediciones ni otra fuente que permita verificar su frecuencia e impacto.
3. El tesista ya no tiene acceso al sistema bancario, a su código, a sus datos ni a su entorno operacional.

## Tipología preliminar por determinar

La propuesta podría adoptar la forma de un modelo arquitectónico, un marco arquitectónico o una composición de modelo, principios arquitectónicos e instancia experimental. La intención de comparar dos arquitecturas requiere, como mínimo, representaciones comparables y una instancia reducida que permita someter ambas a los mismos escenarios de fallo. La tipología no se elegirá por el nombre deseado, sino por los componentes que se diseñarán y la evidencia que pueda obtenerse.

## Condición de factibilidad

Debido a las fechas planificadas, no es metodológicamente válido plantear la mejora del sistema bancario histórico: no puede demostrarse su línea base ni introducirse la propuesta en ese entorno. La idea solo puede conservarse si se abstrae como una clase de problemas y se construye una instancia experimental mínima con servicios simulados, fallos controlados y datos sintéticos. También se requieren practicantes independientes para una evaluación formativa que produzca un rediseño documentado.

Con 15 horas semanales, construir dos pasarelas completas no es viable. La comparación tendría que limitarse a un flujo crítico y a un conjunto pequeño de escenarios de fallo definidos antes de la construcción. Una arquitectura hipotética sin aplicación y sin evidencia no satisface el estándar de maestría en Design Science.

## Recursos de evaluación y construcción

El tesista informa que puede contar con dos ingenieros de software y que puede construir el laboratorio con asistencia de inteligencia artificial. Todavía deben precisarse la experiencia, independencia y disponibilidad de ambos profesionales. La asistencia de inteligencia artificial puede reducir trabajo mecánico de programación, pero no constituye evidencia, participante de evaluación ni sustituto del dominio técnico del investigador; todo código y toda decisión deberán ser comprendidos y verificados por el tesista.

El tesista acepta limitar el estudio a transferencias salientes y a cuatro escenarios: desconexión, pérdida de respuesta, entrega duplicada e indisponibilidad temporal. Puede reconstruir conceptualmente la arquitectura anterior a partir de su memoria o de un documento antiguo. Ese documento solo podrá utilizarse si se acredita que puede emplearlo y si se eliminan datos confidenciales, nombres y detalles propietarios; de lo contrario, la arquitectura de comparación deberá declararse como una referencia reconstruida, no como la arquitectura de una entidad identificable.

## Temas candidatos

### Tema 1. Arquitectura de referencia para transferencias salientes tolerantes a fallos transitorios

- **Problema de diseño tentativo:** un servicio intermediario que envía una transferencia a una entidad remota puede perder la respuesta, recibir mensajes duplicados o encontrar indisponibilidad temporal, dejando el resultado transaccional incierto.
- **Artefacto probable:** composición de modelo arquitectónico, principios de diseño e instancia experimental mínima en .NET Core.
- **Clase de contextos:** integraciones financieras entre sistemas independientes que no comparten una transacción atómica.
- **Relevancia práctica:** alta respecto a la experiencia del tesista, pero todavía sustentada solo por testimonio y no por datos del sistema histórico.
- **Sustentación teórica:** confiabilidad y sus subcaracterísticas en ISO/IEC 25010:2023; fundamentos de sistemas de datos confiables en Kleppmann y Riccomini (2026); transacciones compensatorias en García-Molina y Salem (1987).
- **Contribución potencial:** principios arquitectónicos para mantener trazabilidad, evitar efectos duplicados y llevar cada solicitud a un estado terminal o reconciliable ante fallos transitorios.
- **Factibilidad:** media; exige una instancia reducida y métricas definidas antes de construir.

### Tema 2. Modelo de estados y recuperación para transferencias con resultado incierto

- **Problema de diseño tentativo:** cuando se pierde una respuesta después de enviar una transferencia, el sistema local no puede distinguir inmediatamente entre una operación no procesada y una operación procesada cuya confirmación se perdió.
- **Artefacto probable:** modelo de estados transaccionales y reglas de recuperación, acompañado por una instancia de demostración.
- **Clase de contextos:** operaciones financieras remotas con confirmaciones tardías, perdidas o duplicadas.
- **Relevancia práctica:** alta para la retención descrita por el tesista; requiere traducir el testimonio a escenarios reproducibles.
- **Sustentación teórica:** transacciones distribuidas y compensación en García-Molina y Salem (1987), junto con consistencia y tolerancia a fallos en Kleppmann y Riccomini (2026).
- **Contribución potencial:** principios para representar resultados inciertos y decidir entre consulta, reintento, compensación y reconciliación sin duplicar efectos.
- **Factibilidad:** media-alta si se limita a un único flujo; su evaluación representacional necesita escenarios y revisión experta, no solo pruebas de código.

### Tema 3. Método de pruebas por inyección de fallos para arquitecturas de transferencia

- **Problema de diseño tentativo:** las pruebas funcionales convencionales no exponen sistemáticamente fallos parciales en llamadas entre servicios, por lo que una arquitectura puede parecer correcta sin haber sido sometida a pérdida de respuesta, duplicación o indisponibilidad temporal.
- **Artefacto probable:** método de definición y ejecución de escenarios de fallo, más una instancia experimental en .NET Core.
- **Clase de contextos:** servicios de transferencia o pago que dependen de sistemas remotos.
- **Relevancia práctica:** media-alta y evaluable sin acceso al banco, aunque desplaza el foco desde la arquitectura hacia su verificación.
- **Sustentación teórica:** evaluación de confiabilidad conforme a ISO/IEC 25010:2023 e inyección de fallos a nivel de servicio según Meiklejohn et al. (2021).
- **Contribución potencial:** principios para seleccionar fallos, observar efectos transaccionales y determinar si una arquitectura satisface propiedades de recuperación e integridad.
- **Factibilidad:** alta respecto a los otros temas, pero se aleja parcialmente de la intención original del tesista.

## Comparación preliminar

El Tema 1 preserva mejor la intención arquitectónica, pero también exige más construcción. El Tema 2 concentra la investigación en el núcleo del problema —el resultado incierto— y parece más viable dentro del plazo. El Tema 3 ofrece la evaluación más controlable, aunque convierte la arquitectura en objeto de prueba y no en el artefacto principal.

## Selección provisional

El tesista selecciona el **Tema 1: arquitectura de referencia para transferencias salientes tolerantes a fallos transitorios**.

El tesista rectifica la disponibilidad de participantes: no puede asegurar evaluadores adicionales y solo cuenta con el tutor presencial. Design Science no exige en todos los estudios una evaluación humana externa, pero sí evidencia que no dependa exclusivamente del juicio del constructor. Para este tema se considera la siguiente estrategia mínima:

- evaluación formativa técnica mediante escenarios de desarrollo, seguida de cambios documentados; el tutor revisará la coherencia metodológica y el fundamento de las decisiones, pero no será presentado como evaluador independiente;
- verificación técnica interna de ambas instancias;
- evaluación sumativa experimental con escenarios de fallo y criterios fijados antes de construir;
- separación entre escenarios utilizados durante el diseño y escenarios reservados para la evaluación final;
- repetición de ejecuciones y conservación de registros generados automáticamente.

Esta estrategia reduce el sesgo del constructor mediante criterios previos, datos automáticos y separación de escenarios, pero no reemplaza una evaluación naturalista con practicantes. La tesis deberá declarar como limitación que la utilidad y transferibilidad se evaluaron en un entorno sintético, no en operación bancaria real. En consecuencia, los principios resultantes solo podrán reclamarse para la clase restringida de escenarios reproducidos.

El tesista afirma que el documento histórico es suyo. La posesión del archivo no demuestra que esté autorizado a divulgar contenido producido durante una relación laboral. Para evitar ese riesgo, no se utilizará como fuente ni se reproducirá; la arquitectura de comparación será una referencia genérica construida desde cero, sin identificar a la entidad ni incorporar materiales internos.

## Cierre de E1

El tema seleccionado no depende del acceso al sistema bancario anterior: se construirá y evaluará en un laboratorio controlado disponible para el tesista. E1 queda cerrada con una limitación explícita: la ausencia de evaluación naturalista obliga a restringir la transferibilidad reclamada. La definición de atributos de calidad, criterios y métricas se traslada a E2 y E6; deberán quedar fijados antes de la construcción.
