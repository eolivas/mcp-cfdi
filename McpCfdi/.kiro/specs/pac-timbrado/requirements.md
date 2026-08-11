# Requirements Document

## Introduction

Este documento define los requerimientos para la integración con un Proveedor Autorizado de Certificación (PAC) para timbrar CFDIs pre-sellados, cancelar CFDIs timbrados y consultar su estatus ante el SAT. El sistema recibe un XML de CFDI 4.0 ya firmado con el CSD del emisor (salida del feature cfdi-generation), lo envía al PAC activo para obtener el Timbre Fiscal Digital (UUID, sello SAT, fecha de timbrado), y retorna el CFDI timbrado completo.

La implementación inicial conecta con Multifacturas (API REST JSON, modelo prepago sin suscripción), pero la arquitectura debe soportar PACs alternativos (SW Sapien, FiscalCloud, Finkok, etc.) mediante configuración sin recompilar.

## Glossary

- **PAC**: Proveedor Autorizado de Certificación — entidad autorizada por el SAT para timbrar y certificar CFDIs.
- **Timbrado**: Proceso mediante el cual el PAC valida un CFDI y le asigna un Timbre Fiscal Digital con UUID y sello del SAT.
- **Timbre Fiscal Digital (TFD)**: Complemento XML que contiene UUID, fecha de timbrado, sello del SAT y número de certificado SAT.
- **UUID**: Folio fiscal único de 36 caracteres asignado a cada CFDI timbrado.
- **CSD**: Certificado de Sello Digital — par de llave privada (.key) y certificado (.cer) emitido por el SAT para firmar CFDIs.
- **Sello**: Firma digital SHA-256/RSA del CFDI generada con la llave privada del CSD del emisor.
- **Acuse de cancelación**: Documento XML firmado por el SAT que confirma la recepción de una solicitud de cancelación.
- **Multifacturas**: PAC seleccionado como implementación inicial — modelo prepago sin suscripción mensual con API REST JSON.
- **Strategy Pattern**: Patrón de diseño que permite seleccionar la implementación del PAC en tiempo de configuración.
- **Circuit Breaker**: Patrón de resiliencia que corta llamadas al PAC tras fallos consecutivos para evitar cascadas.

## Requirements

### Requerimiento 1: Timbrado de CFDI pre-sellado

**Historia de usuario:** Como agente IA (MCP client), quiero enviar un XML de CFDI pre-sellado al PAC para obtener el Timbre Fiscal Digital, para que el CFDI tenga validez fiscal ante el SAT.

#### Criterios de aceptación

1. WHEN se envía un XML de CFDI 4.0 previamente sellado (con atributos `Sello`, `NoCertificado`, `Certificado` asignados), THE sistema SHALL enviarlo al PAC activo y retornar el Timbre Fiscal Digital.
2. THE sistema SHALL retornar el XML completo del CFDI timbrado incluyendo el nodo `tfd:TimbreFiscalDigital` con `UUID`, `FechaTimbrado`, `SelloCFD`, `NoCertificadoSAT`, `SelloSAT` y `Version="1.1"` todos no vacíos.
3. THE sistema SHALL extraer y retornar como datos estructurados: UUID, fecha de timbrado, sello del SAT, y número de certificado del SAT.
4. WHEN el XML de entrada NO contiene los atributos `Sello`, `NoCertificado` o `Certificado` con valores no vacíos, THE sistema SHALL rechazar la solicitud con error descriptivo SIN realizar llamada HTTP al PAC.
5. THE sistema SHALL soportar el timbrado de cualquier tipo de CFDI 4.0 (Ingreso, Egreso, Traslado, Pago).

### Requerimiento 2: Cancelación de CFDI

**Historia de usuario:** Como agente IA (MCP client), quiero cancelar un CFDI previamente timbrado ante el SAT, para que el comprobante deje de tener validez fiscal.

#### Criterios de aceptación

1. WHEN se solicita la cancelación proporcionando UUID, RFC emisor, motivo (01, 02, 03, 04) y opcionalmente UUID de sustitución, THE sistema SHALL enviar la solicitud al PAC y retornar el resultado.
2. THE sistema SHALL retornar el acuse de cancelación del SAT (XML del acuse) y el estatus resultante del folio.
3. WHEN el motivo es "01", THE sistema SHALL validar que se proporcione un UUID de sustitución válido (formato GUID). Si falta, SHALL rechazar con error descriptivo.
4. THE sistema SHALL mapear los códigos de respuesta del PAC (201=exitosa, 202=previamente cancelado, 203=no corresponde al emisor, etc.) a un resultado estructurado.

### Requerimiento 3: Consulta de estatus de CFDI

**Historia de usuario:** Como agente IA (MCP client), quiero consultar el estatus de un CFDI ante el SAT, para verificar si está vigente, cancelado o no encontrado.

#### Criterios de aceptación

1. WHEN se proporciona RFC emisor, RFC receptor, Total y UUID, THE sistema SHALL consultar el estatus del CFDI ante el SAT vía el PAC.
2. THE sistema SHALL retornar el estatus (Vigente, Cancelado, No encontrado), estado de cancelación (si aplica), y si es cancelable.

### Requerimiento 4: Abstracción de PAC (Strategy)

**Historia de usuario:** Como desarrollador, quiero que el PAC sea intercambiable por configuración, para poder cambiar de proveedor sin recompilar ni modificar código de negocio.

#### Criterios de aceptación

1. THE sistema SHALL definir una interfaz `IPacService` que abstraiga las operaciones de timbrado, cancelación y consulta de estatus, independiente del PAC concreto.
2. THE sistema SHALL permitir seleccionar el PAC activo mediante configuración (`appsettings.json`) sin recompilar.
3. THE implementación concreta del PAC (adaptador) SHALL residir en la capa de Infraestructura.
4. THE sistema SHALL permitir registrar múltiples implementaciones de PAC y seleccionar la activa por nombre de configuración.

### Requerimiento 5: Adaptador Multifacturas

**Historia de usuario:** Como sistema, quiero implementar la conexión con el PAC Multifacturas vía su API REST JSON, para poder timbrar y cancelar CFDIs.

#### Criterios de aceptación

1. THE sistema SHALL implementar un adaptador para el PAC Multifacturas que consuma su API REST JSON.
2. THE adaptador SHALL autenticarse con las credenciales configuradas (ApiKey/token según su API).
3. THE adaptador SHALL manejar los códigos de error del PAC y traducirlos a excepciones de dominio específicas.
4. THE adaptador SHALL usar `IHttpClientFactory` con políticas de resiliencia (retry con backoff exponencial para errores transitorios, circuit breaker).
5. THE adaptador SHALL registrar trazas de las llamadas al PAC (request/response) a nivel Debug, y errores a nivel Warning/Error.

### Requerimiento 6: Gestión de credenciales del emisor

**Historia de usuario:** Como sistema, quiero cargar las credenciales CSD del emisor desde archivos locales y variables de entorno, para que el agente IA nunca manipule material criptográfico.

#### Criterios de aceptación

1. THE sistema SHALL cargar el certificado (.cer) y la llave privada (.key) desde una carpeta local organizada por RFC: `{CertificadosDir}/{RFC}/certificado.cer` y `{CertificadosDir}/{RFC}/llave.key`.
2. THE sistema SHALL obtener el password de la llave privada desde la variable de entorno `EMISOR__{RFC}__PASSWORD_LLAVE`.
3. THE sistema SHALL NOT persistir las llaves privadas ni passwords en base de datos ni en logs — solo se cargan en memoria durante la operación.
4. THE sistema SHALL fallar con error descriptivo si no encuentra los archivos del emisor o la variable de entorno del password.
5. THE sistema SHALL soportar múltiples emisores en la misma instancia, cada uno con su propia carpeta y variable de entorno.

### Requerimiento 7: Exposición como herramienta MCP

**Historia de usuario:** Como agente IA, quiero que las operaciones de timbrado, cancelación y consulta estén disponibles como herramientas MCP, para poder invocarlas programáticamente.

#### Criterios de aceptación

1. THE sistema SHALL exponer el timbrado como herramienta MCP (`timbrar_cfdi`) que reciba el XML sellado y retorne el CFDI timbrado.
2. THE sistema SHALL exponer la cancelación como herramienta MCP (`cancelar_cfdi`) que reciba UUID, RFC emisor, motivo y opcionalmente folio de sustitución. Las credenciales CSD se cargan internamente por RFC.
3. THE sistema SHALL exponer la consulta de estatus como herramienta MCP (`consultar_estatus_cfdi`) que reciba RFC emisor, RFC receptor, total y UUID.
4. THE herramientas MCP SHALL retornar errores descriptivos en formato texto cuando el PAC rechace la operación.

### Requerimiento 8: Resiliencia

**Historia de usuario:** Como sistema, quiero manejar fallos transitorios del PAC con reintentos y circuit breaker, para no perder operaciones por indisponibilidad temporal.

#### Criterios de aceptación

1. THE sistema SHALL implementar retry con backoff exponencial (máximo 3 reintentos) para errores HTTP 5xx y timeouts del PAC.
2. THE sistema SHALL implementar circuit breaker que se abra tras 5 fallos consecutivos y se cierre tras 30 segundos.
3. THE timeout de conexión al PAC SHALL ser configurable, con default de 30 segundos.

### Requerimiento 9: Observabilidad

**Historia de usuario:** Como operador del sistema, quiero que cada llamada al PAC genere métricas y trazas, para poder diagnosticar problemas y monitorear la salud de la integración.

#### Criterios de aceptación

1. EACH llamada al PAC SHALL registrar métricas de latencia y resultado (éxito/fallo).
2. THE correlation ID del request original SHALL propagarse en las llamadas al PAC.
3. THE errores del PAC SHALL loggearse con contexto suficiente para diagnóstico (código HTTP, cuerpo de respuesta resumido, UUID involucrado).

### Requerimiento 10: Seguridad

**Historia de usuario:** Como responsable de seguridad, quiero que las credenciales del PAC y del emisor se manejen de forma segura, para prevenir filtraciones de datos sensibles.

#### Criterios de aceptación

1. THE credenciales del PAC (token/usuario/password) SHALL almacenarse en configuración segura (secrets, variables de entorno), nunca en código fuente.
2. THE logs SHALL NOT contener llaves privadas, passwords ni certificados completos del emisor.
3. THE comunicación con el PAC SHALL ser exclusivamente por HTTPS.

### Requerimiento 11: Configuración

**Historia de usuario:** Como operador del sistema, quiero configurar el PAC activo y sus credenciales desde `appsettings.json`, para poder cambiar de proveedor con solo reiniciar la aplicación.

#### Criterios de aceptación

1. THE configuración del PAC activo SHALL seguir la estructura:
  ```json
  {
    "Pac": {
      "ActiveProvider": "Multifacturas",
      "Multifacturas": {
        "BaseUrl": "https://api.multifacturas.com",
        "ApiKey": "...",
        "TimeoutSeconds": 30
      },
      "FiscalCloud": {
        "BaseUrl": "https://api.fiscalcloud.mx",
        "ApiKey": "...",
        "TimeoutSeconds": 30
      }
    },
    "Emisores": {
      "CertificadosDir": "./certs/cfdi",
      "DefaultRfc": "EKU9003173C9"
    }
  }
  ```
2. WHEN se cambia el valor de `ActiveProvider`, THE sistema SHALL usar el PAC correspondiente al reiniciar, sin recompilar ni redesplegar.
3. THE credenciales sensibles (API keys del PAC, passwords de llaves privadas) SHALL configurarse vía variables de entorno en el `mcp.json` del MCP client, no en `appsettings.json`.
