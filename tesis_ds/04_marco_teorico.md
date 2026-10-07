# E4. Marco teórico

## Propósito

El marco teórico fundamentará decisiones del artefacto; no será una descripción general de sistemas distribuidos, banca o microservicios. Cada fuente incluida deberá justificar, cuestionar o delimitar al menos una decisión sobre persistencia, identidad, reintento, recuperación, estados, contratos o evaluación.

## Pregunta de búsqueda

**¿Qué teorías, estándares, resultados empíricos y patrones validados deben guiar el diseño de una arquitectura para que un sistema monolítico integrado con servicios externos mediante mensajería asíncrona conserve solicitudes aceptadas, evite repetir efectos de negocio y recupere resultados inciertos ante fallos parciales?**

Las subpreguntas son:

1. ¿Qué propiedades operacionales componen la confiabilidad relevante para este artefacto?
2. ¿Qué garantías y límites tienen la entrega, las confirmaciones y los reintentos en mensajería asíncrona?
3. ¿Qué mecanismos permiten conservar atómicamente la intención de negocio y el trabajo pendiente?
4. ¿Cómo debe definirse y verificarse la idempotencia cuando pueden duplicarse solicitudes o resultados?
5. ¿Cómo deben representarse resultados inciertos, consultas de estado, reenvíos y conciliación?
6. ¿Qué estrategia experimental permite atribuir los resultados a decisiones arquitectónicas específicas?

## Protocolo de búsqueda

### Fuentes de búsqueda

Se consultarán IEEE Xplore, ACM Digital Library, SpringerLink, USENIX, sitios oficiales de estándares y editoriales académicas. La búsqueda web se utilizará para localizar las fuentes, pero las referencias se verificarán en el editor, DOI, repositorio institucional del autor u organismo normalizador.

### Cadenas iniciales

- `"asynchronous messaging" AND idempotency AND retry AND reliability`
- `"transactional outbox" AND evaluation`
- `"message delivery semantics" AND duplicate AND acknowledgment`
- `"microservice resiliency patterns" AND retry AND circuit breaker`
- `"idempotence" AND stateful AND failure AND retry`
- `"service-level fault injection" AND resilience`
- `"uncertain outcome" AND distributed transaction AND reconciliation`

### Criterios de inclusión

- Fuentes primarias, revisiones sistemáticas o estándares.
- Publicaciones de 2021–2026 para conocimiento aplicado reciente.
- Obras anteriores únicamente cuando sean fundacionales o no exista reemplazo vigente.
- Relación explícita con al menos una decisión del artefacto o de su evaluación.
- Autor, año, título, fuente y DOI o URL verificables.

### Criterios de exclusión

- Publicaciones comerciales, entradas personales o contenido sin revisión identificable como sustento central.
- Trabajos limitados a seguridad criptográfica, sobrecarga masiva, acreditaciones entrantes o experiencia de usuario.
- Fuentes que describan una tecnología sin explicar garantías, límites o evidencia.
- Duplicados, resúmenes sin acceso a metadatos verificables y afirmaciones de procesamiento exactamente una vez sin delimitar la frontera de la garantía.

### Suficiencia prevista

E4 requerirá al menos **10 fuentes recientes verificadas de 2021–2026**, complementadas por las obras fundacionales estrictamente necesarias. El número no sustituye la prueba de eliminación: una fuente se conservará solo si su ausencia cambiaría una decisión de diseño, un requisito o un método de evaluación.

## Decisiones que requieren fundamento

1. Momento en que una solicitud se considera aceptada y garantía de persistencia asociada.
2. Conservación conjunta del cambio de estado local y del trabajo pendiente de envío.
3. Identidad estable de la operación y procesamiento idempotente de resultados.
4. Clasificación de fallos reintentables, presupuesto de reintentos y espera entre intentos.
5. Consulta por identidad antes de reenviar cuando se pierde una confirmación.
6. Estados terminales, inciertos y conciliables, junto con sus transiciones permitidas.
7. Confirmaciones del intermediario de mensajes, recuperación tras reinicio y tratamiento de entregas repetidas.
8. Inyección controlada de fallos y separación entre escenarios formativos y sumativos.

## Restricciones tecnológicas de la instancia

El tesista tiene experiencia con colas y reintentos mediante Polly, y domina SQL Server y RabbitMQ. La primera instancia se ejecutará con instalación directa para reducir riesgo de configuración. .NET Core, SQL Server, RabbitMQ y Polly no forman parte de la contribución; podrán sustituirse si otro investigador conserva las propiedades expresadas por los principios. Docker queda como mejora opcional posterior a la verificación.

## Base inicial verificada y efecto previsto

- ISO/IEC 25010:2023 aporta el vocabulario de confiabilidad, tolerancia a fallos y recuperabilidad que se traducirá en requisitos y criterios medibles.
- Kleppmann y Riccomini (2026) fundamentan el tratamiento de fallos parciales, consistencia y decisiones en sistemas intensivos en datos; deberán orientar las fronteras de atomicidad y las condiciones de contorno.
- Ding et al. (2023) ofrecen una definición y verificación de idempotencia ante reintentos; su utilidad se probará en la decisión sobre efectos repetidos.
- Mendonça et al. (2020) muestran que reintentos y disyuntores tienen efectos configurables sobre atributos de calidad; esta fuente impide tratar el reintento como una solución universal.
- Meiklejohn et al. (2021) fundamentan la inyección de fallos a nivel de servicio durante el desarrollo; orientarán la evaluación formativa y la selección de puntos de fallo.
- García-Molina y Salem (1987) fueron examinados para evaluar el uso de compensaciones, pero no se incorporan al núcleo del marco porque el alcance actual no diseña una saga ni coordina múltiples transacciones compensatorias.

## Síntesis teórica orientada al diseño

### Confiabilidad como propiedades verificables

La expresión «arquitectura confiable» no constituye un requisito mientras no se descomponga en propiedades observables. ISO/IEC 25010:2023 distingue tolerancia a fallos, disponibilidad, ausencia de fallos y recuperabilidad; para este estudio, la evidencia se concentrará en tolerancia a fallos y recuperabilidad, no en disponibilidad anual. Amiri et al. (2023) muestran que seguridad, latencia y tolerancia a fallos aparecen recurrentemente en la gestión de resiliencia de sistemas distribuidos, pero esta amplitud también confirma la necesidad de delimitar qué dimensiones se evaluarán. Esta base cambia el diseño: el artefacto deberá exponer estados y registros que permitan observar pérdida, duplicación, recuperación y tiempo de despacho, en lugar de ofrecer una afirmación global de robustez.

### Fallo parcial y estado durable

Un sistema distribuido puede completar una operación remota mientras el emisor desconoce el resultado; esta incertidumbre no se resuelve suponiendo que una ausencia de confirmación equivale a rechazo. Kleppmann y Riccomini (2026) sitúan los fallos parciales y la consistencia como decisiones explícitas de diseño. Jia y Witchel (2021) muestran que los registros durables pueden sostener flujos tolerantes a fallos y recuperación de estado, aunque su contexto de ejecución sin servidor no se transfiera directamente al presente artefacto. La implicación es funcional, no tecnológica: una solicitud solo podrá declararse aceptada cuando su identidad, estado y trabajo pendiente puedan recuperarse tras reiniciar el proceso.

### Idempotencia y frontera del efecto

Idempotencia no significa que el transporte entregue una sola copia, sino que repetir una operación no cambie el comportamiento observable respecto de una sola ejecución. Ding et al. (2023) formalizan la consistencia idempotente y muestran que las operaciones con estado requieren identificar qué acciones deben registrarse para tolerar reejecuciones. Whitaker et al. (2025) combinan administración de idempotencia y finalización de operaciones para permitir reintentos seguros y resolver fallos funcionales en otro tipo de infraestructura. Para este diseño, la identidad estable deberá abarcar solicitud, consulta, reenvío y resultado; el registro de una identidad y la aplicación de su efecto deberán impedir que dos entregas concurrentes superen una comprobación separada del cambio de estado.

### Confirmaciones, reentrega y garantías

RabbitMQ ofrece confirmaciones al publicador y reconocimientos del consumidor, pero la garantía de entrega al menos una vez admite reentregas y duplicados cuando una confirmación se pierde o un consumidor falla (RabbitMQ, s. f.-a, s. f.-b). La marca de reentrega es una pista y no una prueba completa de duplicación. Esta limitación cambia el diseño: el consumidor no dependerá de la marca del intermediario para decidir si aplica el efecto, sino de una identidad de negocio persistente. Asimismo, una publicación no se considerará segura solo porque la llamada del cliente terminó sin error; la instancia deberá observar la confirmación del intermediario y conservar lo no confirmado.

### Reintento como política acotada

El reintento solo es apropiado para fallos clasificados como transitorios y para operaciones cuya repetición sea segura. Mendonça et al. (2020) demuestran que los parámetros de reintento y disyuntor modifican atributos de calidad, mientras Aderaldo et al. (2025) evidencian en C# y Java que su impacto sobre el rendimiento empeora cuando aumenta la tasa de fallos. La documentación vigente de .NET recomienda espera exponencial con variación aleatoria y separa el propósito de reintento del disyuntor (Microsoft, s. f.-b). Por ello, el artefacto no aplicará reintentos indiscriminados: ante una confirmación perdida consultará por identidad; ante cierre explícito diferirá el envío; y solo repetirá con la misma identidad bajo un contrato remoto que reconozca duplicados.

### Distribución como costo, no como objetivo

Talaver y Vakaliuk (2023) advierten que convertir un monolito en microservicios como respuesta general introduce complejidad de comunicación y datos. Esta tensión es central para la tesis: el problema no exige fragmentar el monolito, sino hacer confiable su frontera asíncrona con un servicio externo. En consecuencia, la arquitectura de referencia podrá añadir componentes especializados, pero no reclamará que la distribución interna sea una condición de resiliencia. Otro investigador podrá aplicar los principios dentro de un monolito modular o mediante procesos separados si conserva las mismas propiedades.

### Evaluación mediante fallos representativos

La inyección de fallos es útil cuando cada perturbación representa una amenaza plausible y existe un estado estable observable. Meiklejohn et al. (2021) muestran que los fallos parciales pueden probarse a nivel de servicio durante el desarrollo; Zhang et al. (2022) advierten que la utilidad depende del realismo del modelo de errores; y Naqvi et al. (2022) estructuran experimentos de caos mediante perturbaciones y observación de recuperación. Aderaldo et al. (2025) añaden que una especificación declarativa de escenarios facilita comparar patrones bajo condiciones controladas. Estas fuentes modifican la evaluación: cada escenario tendrá precondición, punto de inyección, resultado esperado y métricas; no se introducirán fallos aleatorios sin relación con el diagnóstico.

## Principios teóricos provisionales que restringen el diseño

1. **Aceptación durable:** no confirmar al solicitante una operación cuya identidad y estado pendiente no puedan recuperarse.
2. **Identidad estable:** conservar la misma identidad lógica a través de envío, consulta, reenvío y resultado.
3. **Efecto idempotente:** decidir y aplicar el efecto de negocio mediante una operación que no permita que entregas equivalentes produzcan múltiples cambios.
4. **Incertidumbre explícita:** representar la pérdida de confirmación como estado desconocido, no como éxito ni rechazo.
5. **Consulta antes de repetición incierta:** consultar el estado remoto cuando exista esa capacidad; reenviar únicamente con la misma identidad y bajo un contrato compatible.
6. **Reintento clasificado y acotado:** distinguir cierre explícito, fallo transitorio, rechazo definitivo y resultado desconocido; cada clase tendrá una política diferente.
7. **Confirmación observable:** usar confirmaciones y reconocimientos del intermediario sin confundirlos con procesamiento exactamente una vez.
8. **Evaluación trazable:** inyectar solo fallos derivados del modelo de amenazas y registrar la recuperación respecto a criterios previos.

Estos principios son compromisos previos al diseño, no resultados finales. E7 podrá refinarlos durante la evaluación formativa y E10 conservará únicamente los que reciban soporte empírico.

## Prueba de eliminación

- Sin ISO/IEC 25010:2023, «confiable» permanecería como calificativo y no se separarían tolerancia a fallos y recuperabilidad.
- Sin Ding et al. (2023) y Whitaker et al. (2025), la duplicidad podría tratarse solo como filtrado de mensajes y no como unicidad del efecto de negocio.
- Sin Jia y Witchel (2021), la recuperación durable se reduciría a disponibilidad del intermediario y omitiría el estado local recuperable.
- Sin Mendonça et al. (2020), Aderaldo et al. (2025) y Microsoft (s. f.-b), se elegiría un reintento genérico sin presupuesto, clasificación ni medición de impacto.
- Sin RabbitMQ (s. f.-a, s. f.-b), la instancia podría confundir confirmación del intermediario con unicidad de procesamiento.
- Sin Meiklejohn et al. (2021), Zhang et al. (2022) y Naqvi et al. (2022), los fallos experimentales no tendrían una estructura reproducible ni un criterio explícito de realismo.
- Sin Talaver y Vakaliuk (2023), la propuesta podría asumir injustificadamente que distribuir el monolito es una mejora por sí misma.

## Estado de la puerta de E4

La pregunta y el protocolo están definidos. Se verificaron **11 fuentes recientes de 2021–2026** relevantes para el fundamento aplicado, además de fuentes metodológicas y antecedentes necesarios. Cada decisión significativa tiene una fuente que modifica el diseño o la evaluación, y el marco supera la prueba de eliminación. E4 queda cerrada; los mecanismos concretos se compararán en E5 y E7, sin convertir las tecnologías elegidas en principios universales.
