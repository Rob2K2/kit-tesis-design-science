# E7b. Requisitos del artefacto

## Función de la etapa

Los requisitos fijan qué debe hacer la instancia y en qué condiciones, antes de diseñar sus componentes. Cada uno procede de una decisión ya confirmada. Las métricas son los indicadores de E6 o conteos derivados de ellos. No se añadirán umbrales después de observar ejecuciones.

## Requisitos funcionales

| Identificador | Propiedad | Métrica | Fuente | Prioridad |
|:--|:--|:--|:--|:--|
| RF-01 | Durante el cierre, aceptar la solicitud, retener los fondos y no despacharla | I4 = 0 y la solicitud tiene registro recuperable | Cierre programado de cinco minutos; I1 e I4 | Alta |
| RF-02 | Comunicar aceptación pendiente sin pedir otra acción al usuario | Reenvíos manuales = 0 | Criterio de conservación durante el cierre | Alta |
| RF-03 | Iniciar el despacho como máximo 10 segundos después de la reapertura | I5 ≤ 10 segundos | E6 | Alta |
| RF-04 | Un rechazo de validación inicial no retiene fondos | Retenciones de solicitudes rechazadas al inicio = 0 | Regla confirmada del flujo | Alta |
| RF-05 | Un rechazo final libera la retención | Rechazos finales con retención todavía vigente = 0 | Regla confirmada del flujo | Alta |
| RF-06 | Usar una identidad de transferencia, creada por el origen antes del primer envío, en el envío, la consulta, el reenvío y el resultado | Reintentos que crean otra identidad = 0 | Principio de identidad estable; confirmación del 13 de octubre de 2026 | Alta |
| RF-07 | Si 1B no llega, esperar 10 segundos y consultar por esa identidad | I6 = 10 segundos | E6 | Alta |
| RF-08 | Si la consulta indica que la cámara ya recibió la solicitud, esperar el resultado final y no reenviar | Reenvíos en ese caso = 0 | Confirmación del 13 de octubre de 2026 | Alta |
| RF-09 | Si la consulta indica que la solicitud no existe, reenviar una sola vez la misma identidad | Reenvíos = 1 y la identidad se mantiene | Confirmación del 13 de octubre de 2026 | Alta |
| RF-10 | Si la consulta no responde, dejar la operación en conciliación: conservar identidad, monto y retención; no debitar, no liberar y no reenviar | I3 cuenta ese estado; efectos automáticos posteriores = 0 | Confirmación del 13 de octubre de 2026 | Alta |
| RF-11 | Registrar la identidad y aplicar el efecto de negocio en la misma transacción | I2 = 0 en la variante propuesta | Resultado duplicado; E7a | Alta |
| RF-12 | Registrar transferencia, retención y mensaje pendiente en la misma transacción, y publicar solo después de confirmarla | I1 = 0 en la variante propuesta | Fallo local; E7a | Alta |
| RF-13 | Dejar toda solicitud aceptada debitada, rechazada con retención liberada o en conciliación | I3 = 1 | E6 | Alta |
| RF-14 | Exponer los estados recibida, rechazada sin retención, fondos retenidos, enviada, resultado pendiente, debitada, retención liberada y conciliación | Toda solicitud aceptada tiene uno de esos estados finales o intermedios registrados | Modelo de estados de E2 | Alta |
| RF-15 | Cambiar por configuración solo el mecanismo comparado en cada ejecución | Cuentas, simulador y registro permanecen iguales entre las dos respuestas | E7a | Alta |
| RF-16 | Mantener la sobrecarga de la aceptación local, sin fallos, en un máximo de 20% | I7 ≤ 0,20 | E6 | Media |

La conciliación de RF-10 no corrige sola la diferencia con la cámara. Conserva la evidencia para compararla después mediante la misma identidad. Mientras tanto la retención sigue vigente.

## Requisitos contextuales

| Identificador | Condición | Métrica | Fuente | Prioridad |
|:--|:--|:--|:--|:--|
| RC-01 | El simulador ofrece consulta por identidad y reconoce un reenvío con esa misma identidad | El escenario de confirmación perdida puede ejecutarse | Condición de contorno de E7a | Alta |
| RC-02 | La evaluación usa datos sintéticos en una instancia local y no incorpora documentos ni identificadores del banco histórico | Ningún nombre o código propietario de la entidad aparece en el artefacto | Delimitación de E2 | Alta |
| RC-03 | La evaluación sumativa cubre solo los cuatro escenarios seleccionados | No se agregan fallos al conjunto sumativo | E3 y E6 | Alta |
| RC-04 | El vehículo es SQL Server, RabbitMQ y Polly. El código propio implementa las decisiones comparadas | La instancia no delega esas decisiones en MassTransit, NServiceBus, CAP, Wolverine, Temporal, Dapr ni Durable Task | E7a | Alta |
| RC-05 | Polly solo reintenta fallos ya clasificados como transitorios | Un silencio de 1B no se trata como un reintento ciego | E4 y E7a | Alta |

Si RC-01 no se cumple, RF-08 y RF-09 no pueden ejecutarse. La propuesta debe terminar en RF-10 y el informe debe declarar esa condición.

## Tensiones y prioridad

La aceptación durable y la transacción del trabajo pendiente aumentan la escritura local. RF-01 y RF-12 pueden empujar la aceptación por encima del 20% de RF-16. Los dos umbrales siguen vigentes. Si la evaluación formativa muestra un exceso, el rediseño debe reducir esa sobrecarga sin abandonar la conservación de la solicitud. Si no lo consigue, el resultado sumativo informará el incumplimiento de RF-16. No se modificará ninguno de los dos valores después de ver los datos.

La consulta reduce el riesgo de un segundo efecto y agrega una espera de 10 segundos. Esa espera es el requisito RF-07, no una demora accidental. El reenvío único de RF-09 solo ocurre cuando la consulta afirma que la solicitud no existe.

## Cierre de E7b

Cada requisito tiene métrica y fuente en el problema, el diagnóstico o una decisión confirmada. Los requisitos cubren el modelo de estados, los cuatro mecanismos de la propuesta, los controles por configuración y las condiciones del simulador. E7b queda cerrada el 13 de octubre de 2026.
