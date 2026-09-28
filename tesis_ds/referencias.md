# Referencias

## Referencias citadas

- Amiri, Z., Heidari, A., Jafari Navimipour, N., & Unal, M. (2023). Resilient and dependability management in distributed environments: A systematic and comprehensive literature review. *Cluster Computing, 26*(2), 1565–1600. https://doi.org/10.1007/s10586-022-03738-5 — **Verificada**.
- Backman, A., Richer, J., & Sporny, M. (2024). *HTTP message signatures* (RFC 9421). Internet Engineering Task Force. https://doi.org/10.17487/RFC9421 — **Verificada**.
- Ding, H., Wang, Z., Shen, Z., Chen, R., & Chen, H. (2023). Automated verification of idempotence for stateful serverless applications. En *17th USENIX Symposium on Operating Systems Design and Implementation (OSDI 23)* (pp. 887–910). USENIX Association. https://www.usenix.org/conference/osdi23/presentation/ding — **Verificada**.
- García-Molina, H., & Salem, K. (1987). Sagas. En *Proceedings of the 1987 ACM SIGMOD International Conference on Management of Data* (pp. 249–259). Association for Computing Machinery. https://doi.org/10.1145/38713.38742 — **Verificada**.
- Gregor, S., & Hevner, A. R. (2013). Positioning and presenting design science research for maximum impact. *MIS Quarterly, 37*(2), 337–355. https://doi.org/10.25300/MISQ/2013/37.2.01 — **Verificada**.
- International Organization for Standardization. (2023). *Systems and software engineering—Systems and software Quality Requirements and Evaluation (SQuaRE)—Product quality model* (ISO/IEC Standard No. 25010:2023). https://www.iso.org/standard/78176.html — **Verificada**.
- Jones, M., Bradley, J., & Sakimura, N. (2015). *JSON Web Signature (JWS)* (RFC 7515). Internet Engineering Task Force. https://doi.org/10.17487/RFC7515 — **Verificada**.
- Kleppmann, M., & Riccomini, C. (2026). *Designing data-intensive applications: The big ideas behind reliable, scalable, and maintainable systems* (2.ª ed.). O’Reilly Media. https://martin.kleppmann.com/2026/03/24/designing-data-intensive-applications-2e.html — **Verificada**.
- Meiklejohn, C. S., Estrada, A., Song, Y., Miller, H., & Padhye, R. (2021). Service-level fault injection testing. En *Proceedings of the ACM Symposium on Cloud Computing* (pp. 388–402). Association for Computing Machinery. https://doi.org/10.1145/3472883.3487005 — **Verificada**.
- Mendonça, N. C., Aderaldo, C. M., Cámara, J., & Garlan, D. (2020). Model-based analysis of microservice resiliency patterns. En *2020 IEEE International Conference on Software Architecture (ICSA)* (pp. 114–124). IEEE. https://doi.org/10.1109/ICSA47634.2020.00018 — **Verificada**.
- Peffers, K., Tuunanen, T., Rothenberger, M. A., & Chatterjee, S. (2007). A design science research methodology for information systems research. *Journal of Management Information Systems, 24*(3), 45–77. https://doi.org/10.2753/MIS0742-1222240302 — **Verificada**.
- Venable, J., Pries-Heje, J., & Baskerville, R. (2016). FEDS: A framework for evaluation in design science research. *European Journal of Information Systems, 25*(1), 77–89. https://doi.org/10.1057/ejis.2014.36 — **Verificada**.
- Wieringa, R. J. (2014). *Design science methodology for information systems and software engineering*. Springer. https://doi.org/10.1007/978-3-662-43839-8 — **Verificada**.

## Bibliografía de consulta

No se mantienen entradas de consulta sin citar en esta etapa.

## Notas de búsqueda de E1

- **Pregunta:** ¿qué fuentes verificables pueden sustentar la delimitación de confiabilidad, las transacciones distribuidas y una evaluación experimental mediante fallos controlados?
- **Cadenas principales:** “ISO IEC 25010 2023 product quality reliability”; “Garcia-Molina Salem Sagas 1987 ACM DOI”; “Service-Level Fault Injection Testing SoCC 2021 DOI”; “Designing Data-Intensive Applications second edition 2026”.
- **Criterios de inclusión:** norma vigente; fuente primaria o sitio oficial del autor/editor; relación directa con confiabilidad, transacciones distribuidas o inyección de fallos; datos bibliográficos verificables.
- **Criterios de exclusión:** publicaciones comerciales sin autoría o revisión identificable; afirmaciones técnicas sin fuente primaria; entradas que prometen garantías de procesamiento exactamente una vez sin delimitar el nivel de la garantía.
- **Hallazgo negativo:** la búsqueda exploratoria específica sobre idempotencia en pagos devolvió principalmente publicaciones técnicas no arbitradas. No se usarán como sustento académico central hasta localizar y verificar fuentes primarias apropiadas.

## Notas de búsqueda de E2

- **Pregunta:** ¿qué mecanismos normalizados permiten proteger la integridad y autenticar el origen de mensajes JSON o HTTP de manera análoga a la firma digital aplicada a mensajes XML?
- **Cadenas principales:** “RFC JSON Web Signature 7515 integrity digital signature”; “RFC 9421 HTTP Message Signatures”.
- **Resultado:** JWS protege mediante firma digital o código de autenticación una secuencia arbitraria de octetos; HTTP Message Signatures permite firmar componentes seleccionados de un mensaje HTTP. Ninguno se evaluará como parte del artefacto, porque la seguridad criptográfica queda como precondición externa.
- **Pregunta complementaria:** ¿qué evidencia académica existe sobre reintentos, idempotencia y evaluación de patrones de resiliencia en sistemas distribuidos?
- **Cadenas principales:** “microservice resilience patterns systematic literature review retries circuit breaker idempotency DOI”; “distributed payment systems duplicate messages idempotency architecture academic paper”; “Automated Verification of Idempotence OSDI 2023”.
- **Resultado provisional:** se verificaron estudios generales sobre gestión de resiliencia, análisis de patrones de reintento y verificación de idempotencia. La búsqueda exploratoria no identificó todavía una evaluación primaria de la combinación específica de cierre programado y respuestas satisfactorias duplicadas en transferencias interinstitucionales.
- **Verificación metodológica de E3:** se confirmaron los datos bibliográficos y DOI del DSRM de Peffers et al. (2007), FEDS de Venable et al. (2016), los ciclos de Wieringa (2014) y el marco de contribuciones de Gregor y Hevner (2013).
