# E7e. Construcción y verificación interna

## Función de la etapa

E7e registra lo que se construye, lo que se verifica y lo que cambia. No anticipa resultados de la evaluación formativa ni de las 240 ejecuciones. Al abrirla, el 18 de octubre de 2026, no existe código de la instancia.

## Entorno comprobado

| Recurso | Comprobación | Consecuencia |
|:--|:--|:--|
| SDK de .NET | Versión 10.0.401, disponible en la máquina | La instancia se compilará con el SDK instalado. El objetivo de compilación previsto es `net10.0` |
| SQL Server | Instancia local `MSSQLSERVER` en ejecución. Edición Developer 2022, versión 16.0.1210.3. La autenticación de Windows conecta | El libro puede crearse en esta instancia |
| RabbitMQ | Servicio `RabbitMQ` en ejecución. Versión 4.3.6. El puerto 5672 responde en esta máquina | El intermediario local ya puede usarse en V0 |

Estos hechos se observaron en la máquina de construcción. Erlang/OTP 28.5 se instaló porque RabbitMQ 4.3.6 admite Erlang 27 y 28. Erlang 29 exige RabbitMQ 4.3.7 o posterior, así que no se instaló. El instalador oficial de RabbitMQ 4.3.6 dejó el servicio en ejecución.

## Orden de construcción de V0

1. Crear la solución con tres proyectos de consola —origen, cámara y ejecutor— y una biblioteca compartida de mensajes y estados.
2. Crear el libro: transferencia, retención, trabajo pendiente, identidad procesada, efecto, ventana y ejecución.
3. Implementar el camino sin fallos de la propuesta: aceptar, retener, dejar el trabajo pendiente en la misma transacción, publicar, esperar la confirmación del publicador, recibir la confirmación inicial y aplicar el resultado.
4. Verificar que, al detener el origen después de confirmar la transacción, el libro conserva la solicitud.
5. Verificar una ejecución en la que el intermediario ya confirmó la publicación y la confirmación inicial todavía no llegó.

Los contrastes, el punto de caída evaluado y Polly esperan a V1 o V3, según E7d.

## Verificación interna prevista

Antes de llamar a V0 terminada se exigirá:

- la solución compila;
- el esquema se crea en la instancia local;
- una solicitud de prueba recorre el camino sin fallos;
- el reinicio del origen no borra una transacción ya confirmada;
- el registro distingue la confirmación del publicador de la confirmación inicial.

No se informarán indicadores de E6 como resultados de esta verificación.

## Registro de construcción

| Fecha | Cambio | Motivo | Verificación |
|:--|:--|:--|:--|
| 18 de octubre de 2026 | Apertura de la etapa. No se escribió código | El entorno se comprobó antes de crear la solución | SQL Server responde. RabbitMQ todavía no estaba disponible |
| 18 de octubre de 2026 | Instalación de Erlang/OTP 28.5 y RabbitMQ 4.3.6. El tesista fijó `artefacto/` como carpeta del código | El tesista pidió instalar el intermediario en esta máquina | El servicio `RabbitMQ` está en ejecución y el puerto 5672 responde |
| 18 de octubre de 2026 | V0 en `artefacto/`: origen, cámara, ejecutor y biblioteca compartida. Libro `TransferenciasLab` en SQL Server | Camino sin fallos y conservación tras reinicio, según E7d | La solución compila. `verificar-v0` terminó con código 0 |
| 18 de octubre de 2026 | El origen cerraba el lector de solicitudes antes de terminar la transacción | La primera ejecución falló con un lector abierto al confirmar la transacción vacía | La segunda ejecución de `verificar-v0` completó los dos chequeos |
| 19 de octubre de 2026 | V1: la variable `PUBLICACION` elige trabajo pendiente o publicación directa. La caída sigue siendo la salida después de confirmar y antes de publicar | Hacía falta el contraste del fallo local antes de la evaluación formativa | `verificar-v1` terminó con código 0. `verificar-v0` volvió a terminar con código 0 |

## Resultados de la verificación V0

Se ejecutó `dotnet exec Transferencias.Ejecutor.dll verificar-v0` el 18 de octubre de 2026, y de nuevo el 19 de octubre después de incorporar V1. En ambas ocasiones el proceso terminó con código 0.

1. Con la cámara detenida, el origen aceptó 25.00 desde la cuenta `C-100`, confirmó la transacción y recibió la confirmación del publicador. `ConfirmacionInicialEn` seguía vacío. Ese intervalo separa las dos confirmaciones.
2. Al arrancar la cámara, la misma identidad pasó a debitada. El saldo quedó en 975.00, hubo un efecto y la retención dejó de estar vigente.
3. En una segunda pasada, el origen se detuvo después de confirmar la aceptación y antes de publicar. El libro conservó fondos retenidos, retención vigente y trabajo pendiente. `PublicadorConfirmadoEn` estaba vacío.
4. Al reiniciar el origen y arrancar la cámara, esa misma identidad se publicó y terminó debitada.

## Resultados de la verificación V1

`verificar-v1` ejecutó cinco repeticiones internas por respuesta el 19 de octubre de 2026. No forman parte de las 240 ejecuciones sumativas.

| Respuesta | Pérdidas tras la caída y el reinicio | Lectura |
|:--|:--|:--|
| Publicación directa | 5 de 5 | La transferencia y la retención quedan grabadas, pero no hay trabajo pendiente. En ocho segundos el reinicio no publica ni debita |
| Trabajo pendiente | 0 de 5 | El reinicio encuentra el trabajo pendiente, publica y debita la misma identidad |

La mediana del tiempo de aceptación, en cinco pasadas sin caída, fue 97 ms para la publicación directa y 95 ms para el trabajo pendiente. La diferencia es de 2 ms y queda dentro del 20% de I7. Con esta cantidad de pasadas, esa diferencia no autoriza a decir que una respuesta sea más rápida. El umbral de E6 no se modifica.

La respuesta con trabajo pendiente no mostró una falla en este chequeo. La publicación directa queda detenida, con los fondos retenidos y sin despacho. Estas cinco repeticiones comprueban que el punto de caída distingue las dos respuestas. No sustituyen el ciclo formativo con rediseño.

## Límites de lo construido

La consulta todavía no se usa. El acuse se publica y la cámara no lo consume. Ambos procesos usan la misma instancia de SQL Server y tablas distintas. `SolicitudEntrante` es solo el mecanismo del ejecutor para entregar la solicitud sintética. Polly no está incluido. Los contrastes de cierre, resultado duplicado y confirmación perdida siguen fuera. La compilación advierte una vulnerabilidad transitiva de `System.Security.Cryptography.Pkcs` 9.0.4, arrastrada por el cliente de SQL Server; no interviene en el experimento.

## Cierre de E7e

V0 y V1 están construidas. La verificación interna separa la confirmación del publicador, conserva la transacción al reiniciar y separa las dos respuestas del fallo local. E7e queda cerrada el 19 de octubre de 2026. La evaluación formativa, y el rediseño que deba documentarse, corresponden a E8.
