# Requirements Document

## Introduction

Este documento define los requerimientos para la generación de un CFDI (Comprobante Fiscal Digital por Internet) mínimo y válido conforme al Anexo 20 del SAT. El objetivo es producir la estructura XML con todos los campos obligatorios necesarios para que un PAC (Proveedor Autorizado de Certificación) pueda certificar el comprobante. El alcance se limita al tipo de comprobante "Ingreso" (factura comercial) con los nodos requeridos: Emisor, Receptor, Conceptos e Impuestos.

## Glossary

- **CFDI**: Comprobante Fiscal Digital por Internet — documento XML que ampara una transacción fiscal en México.
- **SAT**: Servicio de Administración Tributaria — autoridad fiscal de México.
- **PAC**: Proveedor Autorizado de Certificación — entidad que timbra y certifica un CFDI ante el SAT.
- **Anexo_20**: Especificación técnica oficial del SAT que define la estructura, reglas y catálogos para CFDI versión 4.0.
- **Generador_CFDI**: Componente del sistema responsable de construir el documento XML del CFDI.
- **Validador_CFDI**: Componente del sistema responsable de verificar que un CFDI cumple con las reglas del Anexo 20 antes del timbrado.
- **Serializador_XML**: Componente del sistema responsable de convertir el modelo de dominio del CFDI a su representación XML.
- **Parser_XML**: Componente del sistema responsable de convertir un documento XML de CFDI a su modelo de dominio equivalente.
- **RFC**: Registro Federal de Contribuyentes — identificador fiscal único de personas físicas y morales en México.
- **Régimen_Fiscal**: Clave del catálogo c_RegimenFiscal del SAT que identifica el régimen tributario del contribuyente.
- **Uso_CFDI**: Clave del catálogo c_UsoCFDI que indica el uso que el receptor dará al comprobante.
- **Clave_Producto_Servicio**: Clave del catálogo c_ClaveProdServ del SAT que identifica el bien o servicio.
- **Clave_Unidad**: Clave del catálogo c_ClaveUnidad del SAT que identifica la unidad de medida.
- **Forma_Pago**: Clave del catálogo c_FormaPago que indica cómo se realizó el pago.
- **Método_Pago**: Clave del catálogo c_MetodoPago que indica si el pago es en una sola exhibición (PUE) o en parcialidades (PPD).
- **Moneda**: Clave del catálogo c_Moneda del SAT (ejemplo: MXN, USD).
- **Tipo_Comprobante**: Clave que indica la naturaleza del CFDI (I=Ingreso, E=Egreso, T=Traslado, N=Nómina, P=Pago).
- **Lugar_Expedición**: Código postal del domicilio fiscal desde donde se expide el CFDI.
- **IVA**: Impuesto al Valor Agregado — impuesto indirecto principal en México.
- **Timbrado**: Proceso mediante el cual el PAC certifica el CFDI asignándole un UUID y sello digital.

## Requirements

### Requerimiento 1: Generación de la estructura base del CFDI

**Historia de usuario:** Como sistema emisor de facturas, quiero generar la estructura XML base de un CFDI versión 4.0, para que el comprobante contenga todos los atributos obligatorios del nodo raíz `Comprobante`.

#### Criterios de aceptación

1. WHEN se solicita la generación de un CFDI, THE Generador_CFDI SHALL producir un documento XML con el nodo raíz `Comprobante`, namespace `http://www.sat.gob.mx/cfd/4` y prefijo `cfdi`.
2. THE Generador_CFDI SHALL incluir el atributo `Version` con valor fijo "4.0" en el nodo `Comprobante`.
3. WHEN se proporciona la información de la transacción, THE Generador_CFDI SHALL incluir los atributos obligatorios: `Fecha`, `Sello`, `FormaPago`, `NoCertificado` (exactamente 20 dígitos numéricos), `Certificado`, `SubTotal`, `Moneda`, `Total`, `TipoDeComprobante`, `MetodoPago`, `LugarExpedicion` (código postal de 5 dígitos) y `Exportacion`.
4. THE Generador_CFDI SHALL formatear el atributo `Fecha` en el formato `yyyy-MM-ddTHH:mm:ss` expresado en la hora local correspondiente al código postal del atributo `LugarExpedicion`, sin indicador de zona horaria, conforme al estándar del Anexo_20.
5. IF el comprobante corresponde a una operación nacional sin exportación de mercancías, THEN THE Generador_CFDI SHALL asignar el valor "01" al atributo `Exportacion`.
6. IF alguno de los atributos obligatorios del nodo `Comprobante` no es proporcionado en la información de la transacción, THEN THE Generador_CFDI SHALL rechazar la generación e indicar cuáles atributos obligatorios faltan.

### Requerimiento 2: Información del Emisor

**Historia de usuario:** Como sistema emisor de facturas, quiero incluir la información fiscal del emisor en el CFDI, para que el comprobante identifique correctamente a quien expide la factura.

#### Criterios de aceptación

1. WHEN se genera un CFDI, THE Generador_CFDI SHALL incluir un nodo `cfdi:Emisor` con los atributos obligatorios `Rfc`, `Nombre` y `RegimenFiscal`.
2. THE Validador_CFDI SHALL verificar que el RFC del emisor cumpla con la estructura definida por el SAT: 3 letras (incluyendo Ñ y &) seguidas de 6 dígitos de fecha y 3 caracteres alfanuméricos de homoclave para personas morales (12 caracteres en total), o 4 letras seguidas de 6 dígitos de fecha y 3 caracteres alfanuméricos de homoclave para personas físicas (13 caracteres en total).
3. THE Validador_CFDI SHALL verificar que el atributo `Nombre` del emisor tenga una longitud entre 1 y 254 caracteres, sin espacios al inicio ni al final.
4. THE Validador_CFDI SHALL verificar que el atributo `RegimenFiscal` contenga una clave válida del catálogo c_RegimenFiscal del SAT y que dicha clave sea aplicable al tipo de persona (física o moral) según lo indica el catálogo.
5. IF el RFC del emisor no cumple con la estructura definida en el criterio 2, THEN THE Validador_CFDI SHALL rechazar la generación e indicar un error que especifique que el RFC del emisor tiene un formato inválido.
6. IF el atributo `RegimenFiscal` del emisor no es una clave válida del catálogo c_RegimenFiscal o no es aplicable al tipo de persona derivado del RFC, THEN THE Validador_CFDI SHALL rechazar la generación e indicar un error que especifique el régimen fiscal inválido y el tipo de persona detectado.
7. IF el atributo `Nombre` del emisor está vacío o excede 254 caracteres, THEN THE Validador_CFDI SHALL rechazar la generación e indicar un error que especifique que el nombre del emisor no cumple con la longitud permitida.

### Requerimiento 3: Información del Receptor

**Historia de usuario:** Como sistema emisor de facturas, quiero incluir la información fiscal del receptor en el CFDI, para que el comprobante identifique correctamente a quien recibe la factura.

#### Criterios de aceptación

1. WHEN se genera un CFDI, THE Generador_CFDI SHALL incluir un nodo `cfdi:Receptor` con los atributos obligatorios `Rfc`, `Nombre`, `DomicilioFiscalReceptor`, `RegimenFiscalReceptor` y `UsoCFDI`, donde `Nombre` tiene una longitud entre 1 y 254 caracteres.
2. THE Validador_CFDI SHALL verificar que el RFC del receptor tenga 12 caracteres para personas morales, 13 caracteres para personas físicas, o sea uno de los RFC genéricos: "XAXX010101000" para público en general o "XEXX010101000" para operaciones con extranjeros.
3. THE Validador_CFDI SHALL verificar que el atributo `DomicilioFiscalReceptor` sea un código postal de 5 dígitos numéricos existente en el catálogo c_CodigoPostal del SAT.
4. THE Validador_CFDI SHALL verificar que el atributo `UsoCFDI` contenga una clave válida del catálogo c_UsoCFDI del SAT.
5. THE Validador_CFDI SHALL verificar que el atributo `RegimenFiscalReceptor` contenga una clave válida del catálogo c_RegimenFiscal del SAT.
6. IF el RFC del receptor no cumple con el formato establecido, THEN THE Validador_CFDI SHALL rechazar la generación e indicar el error de formato en el RFC del receptor.
7. IF el atributo `DomicilioFiscalReceptor`, `UsoCFDI` o `RegimenFiscalReceptor` no contiene una clave válida en su catálogo correspondiente, THEN THE Validador_CFDI SHALL rechazar la generación indicando cuál atributo y cuál catálogo falló la validación.

### Requerimiento 4: Conceptos (líneas de detalle)

**Historia de usuario:** Como sistema emisor de facturas, quiero incluir los conceptos (bienes o servicios) en el CFDI, para que el comprobante detalle lo que se está facturando.

#### Criterios de aceptación

1. WHEN se genera un CFDI, THE Generador_CFDI SHALL incluir al menos un nodo `cfdi:Concepto` dentro del nodo `cfdi:Conceptos`.
2. THE Generador_CFDI SHALL incluir en cada `cfdi:Concepto` los atributos obligatorios: `ClaveProdServ`, `Cantidad`, `ClaveUnidad`, `Descripcion`, `ValorUnitario`, `Importe` y `ObjetoImp`, y opcionalmente los atributos `NoIdentificacion` (máximo 100 caracteres) y `Unidad` (máximo 20 caracteres) cuando sean proporcionados en la solicitud.
3. THE Validador_CFDI SHALL verificar que el atributo `ClaveProdServ` contenga una clave válida del catálogo c_ClaveProdServ del SAT.
4. THE Validador_CFDI SHALL verificar que el atributo `ClaveUnidad` contenga una clave válida del catálogo c_ClaveUnidad del SAT.
5. THE Validador_CFDI SHALL verificar que el atributo `Importe` sea igual a `Cantidad` multiplicado por `ValorUnitario`, redondeado a la cantidad de decimales de la moneda correspondiente según el catálogo c_Moneda del SAT.
6. THE Validador_CFDI SHALL verificar que el atributo `ObjetoImp` contenga un valor válido del catálogo c_ObjetoImp ("01" sin impuesto, "02" sí objeto de impuesto, "03" sí objeto y no obligado al desglose).
7. IF el nodo `cfdi:Conceptos` no contiene al menos un `cfdi:Concepto`, THEN THE Validador_CFDI SHALL rechazar la generación indicando que se requiere al menos un concepto.
8. THE Validador_CFDI SHALL verificar que el atributo `Cantidad` sea un valor numérico mayor a cero con un máximo de 6 decimales, y que el atributo `ValorUnitario` sea un valor numérico mayor o igual a cero con un máximo de decimales definido por la moneda del comprobante.
9. THE Validador_CFDI SHALL verificar que el atributo `Descripcion` tenga una longitud mínima de 1 carácter y máxima de 1000 caracteres, sin contener únicamente espacios en blanco.
10. IF algún atributo obligatorio de un `cfdi:Concepto` no cumple con su validación (catálogo inválido, rango numérico fuera de límites, o descripción fuera de longitud permitida), THEN THE Validador_CFDI SHALL rechazar la generación indicando el atributo que falló y el concepto afectado.

### Requerimiento 5: Impuestos trasladados por concepto

**Historia de usuario:** Como sistema emisor de facturas, quiero desglosar los impuestos trasladados a nivel de cada concepto, para que el comprobante refleje correctamente la carga fiscal por línea.

#### Criterios de aceptación

1. WHILE el atributo `ObjetoImp` de un concepto tenga valor "02", THE Generador_CFDI SHALL incluir al menos un nodo `cfdi:Traslado` dentro de `cfdi:Concepto/cfdi:Impuestos/cfdi:Traslados`.
2. IF el atributo `TipoFactor` del traslado es "Tasa" o "Cuota", THEN THE Generador_CFDI SHALL incluir en el nodo `cfdi:Traslado` los atributos: `Base`, `Impuesto`, `TipoFactor`, `TasaOCuota` e `Importe`.
3. IF el atributo `TipoFactor` del traslado es "Exento", THEN THE Generador_CFDI SHALL incluir en el nodo `cfdi:Traslado` únicamente los atributos: `Base`, `Impuesto` y `TipoFactor`, omitiendo `TasaOCuota` e `Importe`.
4. THE Validador_CFDI SHALL verificar que el atributo `Base` del traslado sea mayor a cero y no exceda la suma de `Importe` menos `Descuento` del concepto al que pertenece.
5. IF el atributo `TipoFactor` es "Tasa" o "Cuota", THEN THE Validador_CFDI SHALL verificar que el atributo `Importe` del traslado sea igual a `Base` multiplicado por `TasaOCuota`, redondeado a la cantidad de decimales de la moneda.
6. THE Validador_CFDI SHALL verificar que el atributo `Impuesto` contenga una clave válida del catálogo c_Impuesto del SAT (ejemplo: "002" para IVA).
7. THE Validador_CFDI SHALL verificar que el atributo `TipoFactor` sea "Tasa", "Cuota" o "Exento".
8. THE Validador_CFDI SHALL verificar que el atributo `TasaOCuota` contenga un valor válido del catálogo c_TasaOCuota del SAT correspondiente al impuesto y tipo de factor indicados.
9. IF algún atributo del nodo `cfdi:Traslado` no cumple las validaciones de los criterios 4 a 8, THEN THE Validador_CFDI SHALL rechazar la generación indicando el concepto afectado y el atributo que falló la validación.

### Requerimiento 6: Nodo global de Impuestos

**Historia de usuario:** Como sistema emisor de facturas, quiero incluir el resumen global de impuestos en el CFDI, para que el comprobante totalice correctamente los impuestos trasladados.

#### Criterios de aceptación

1. WHEN el CFDI contiene al menos un concepto con `ObjetoImp` igual a "02", THE Generador_CFDI SHALL incluir el nodo `cfdi:Impuestos` a nivel raíz con el atributo `TotalImpuestosTrasladados`.
2. IF ningún concepto del CFDI tiene `ObjetoImp` igual a "02", THEN THE Generador_CFDI SHALL omitir el nodo `cfdi:Impuestos` a nivel raíz.
3. WHEN se genera el nodo `cfdi:Impuestos` a nivel raíz, THE Generador_CFDI SHALL incluir un nodo `cfdi:Traslado` dentro de `cfdi:Impuestos/cfdi:Traslados` por cada combinación única de `Impuesto`, `TipoFactor` y `TasaOCuota` presente en los traslados a nivel concepto, con los atributos `Base`, `Impuesto`, `TipoFactor`, `TasaOCuota` e `Importe`.
4. IF un traslado global corresponde a una combinación donde `TipoFactor` es "Exento", THEN THE Generador_CFDI SHALL omitir los atributos `TasaOCuota` e `Importe` en ese nodo `cfdi:Traslado` global e incluir únicamente `Base`, `Impuesto` y `TipoFactor`.
5. THE Validador_CFDI SHALL verificar que el atributo `TotalImpuestosTrasladados` sea igual a la suma de todos los atributos `Importe` de los traslados a nivel concepto, redondeado a la cantidad de decimales definida por la moneda en el catálogo c_Moneda del SAT.
6. THE Validador_CFDI SHALL verificar que el atributo `Importe` de cada traslado global sea igual a la suma de los importes de traslados a nivel concepto con la misma combinación de `Impuesto`, `TipoFactor` y `TasaOCuota`, redondeado a la cantidad de decimales definida por la moneda.
7. THE Validador_CFDI SHALL verificar que el atributo `Base` de cada traslado global sea igual a la suma de las bases de traslados a nivel concepto con la misma combinación de `Impuesto`, `TipoFactor` y `TasaOCuota`, redondeado a la cantidad de decimales definida por la moneda.

### Requerimiento 7: Cálculo de totales del comprobante

**Historia de usuario:** Como sistema emisor de facturas, quiero que los totales del CFDI se calculen correctamente, para que el comprobante sea aritméticamente válido y pase la validación del PAC.

#### Criterios de aceptación

1. THE Generador_CFDI SHALL calcular el atributo `SubTotal` como la suma de los atributos `Importe` de todos los nodos `cfdi:Concepto`, redondeando el resultado al número de decimales definido por la moneda en el catálogo c_Moneda del SAT.
2. THE Generador_CFDI SHALL calcular el atributo `Descuento` del nodo `Comprobante` como la suma de los atributos `Descuento` de todos los nodos `cfdi:Concepto` que contengan dicho atributo, redondeando el resultado al número de decimales definido por la moneda.
3. THE Generador_CFDI SHALL calcular el atributo `Total` como: `SubTotal` menos `Descuento` (cuando el atributo `Descuento` está presente en el comprobante) más `TotalImpuestosTrasladados` (cuando el nodo `cfdi:Impuestos` está presente) menos `TotalImpuestosRetenidos` (cuando el atributo `TotalImpuestosRetenidos` está presente en el nodo `cfdi:Impuestos`), redondeando el resultado al número de decimales definido por la moneda.
4. THE Validador_CFDI SHALL verificar que el atributo `SubTotal` sea mayor o igual a cero.
5. IF el atributo `TipoDeComprobante` es "I" (Ingreso), THEN THE Validador_CFDI SHALL verificar que el atributo `Total` sea mayor o igual a cero.
6. THE Validador_CFDI SHALL verificar que los atributos `SubTotal`, `Total` y `Descuento` contengan exactamente el número de decimales definido para la moneda del comprobante en el catálogo c_Moneda del SAT, aplicando redondeo estándar (half-up) al resultado de cada operación aritmética.
7. IF el valor calculado de `SubTotal` no coincide con la suma de los atributos `Importe` de los conceptos, o el valor calculado de `Total` no coincide con la fórmula definida en el criterio 3, THEN THE Validador_CFDI SHALL rechazar el comprobante indicando cuál atributo presenta la discrepancia aritmética y los valores esperado y obtenido.

### Requerimiento 8: Serialización del CFDI a XML

**Historia de usuario:** Como sistema emisor de facturas, quiero serializar el modelo de dominio del CFDI a formato XML, para que el resultado sea un documento conforme al esquema XSD del Anexo_20.

#### Criterios de aceptación

1. WHEN se solicita la serialización de un modelo de dominio CFDI válido, THE Serializador_XML SHALL producir un documento XML que pase la validación contra el esquema XSD `cfdv40.xsd` del SAT sin errores.
2. THE Serializador_XML SHALL incluir la declaración de namespaces requerida: `xmlns:cfdi="http://www.sat.gob.mx/cfd/4"` y `xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"`.
3. THE Serializador_XML SHALL incluir el atributo `xsi:schemaLocation` con el valor `http://www.sat.gob.mx/cfd/4 http://www.sat.gob.mx/sitio_internet/cfd/4/cfdv40.xsd`.
4. THE Serializador_XML SHALL generar el XML sin declaración `<?xml?>` y con codificación UTF-8.
5. THE Serializador_XML SHALL serializar los valores numéricos sin ceros a la izquierda en la parte entera y con el número exacto de decimales definido para la moneda en el catálogo c_Moneda del SAT (por ejemplo, 2 decimales para MXN en importes, hasta 6 decimales para tipo de cambio).
6. THE Serializador_XML SHALL omitir del XML resultante los atributos opcionales cuyo valor no esté presente en el modelo de dominio, sin generar atributos vacíos.
7. IF el modelo de dominio proporcionado no contiene todos los campos obligatorios definidos por el esquema XSD, THEN THE Serializador_XML SHALL rechazar la serialización e indicar cuáles campos obligatorios están ausentes.
8. THE Serializador_XML SHALL generar los nodos hijo del elemento `cfdi:Comprobante` en el orden de secuencia definido por el esquema XSD (`cfdi:InformacionGlobal`, `cfdi:CfdiRelacionados`, `cfdi:Emisor`, `cfdi:Receptor`, `cfdi:Conceptos`, `cfdi:Impuestos`, `cfdi:Complemento`, `cfdi:Addenda`).

### Requerimiento 9: Deserialización de XML a modelo de dominio

**Historia de usuario:** Como sistema emisor de facturas, quiero parsear un documento XML de CFDI de vuelta a su modelo de dominio, para poder leer y procesar CFDIs existentes.

#### Criterios de aceptación

1. WHEN se proporciona un documento XML válido de CFDI 4.0 con namespace `http://www.sat.gob.mx/cfd/4`, THE Parser_XML SHALL producir un modelo de dominio donde cada atributo obligatorio del XML tiene su campo correspondiente poblado con el mismo valor textual presente en el documento fuente.
2. IF el documento XML no está bien formado (XML malformado), THEN THE Parser_XML SHALL retornar un error indicando que el documento no es XML válido.
3. IF el documento XML está bien formado pero no utiliza el namespace `http://www.sat.gob.mx/cfd/4` o carece de atributos obligatorios del nodo Comprobante, THEN THE Parser_XML SHALL retornar un error indicando si la causa es namespace incorrecto o atributos obligatorios faltantes.
4. THE Parser_XML SHALL producir un modelo de dominio que, al ser serializado con el Serializador_XML y parseado nuevamente con el Parser_XML, resulte en un modelo con valores idénticos campo por campo al modelo original para todos los atributos obligatorios y opcionales que estuvieran poblados.
5. WHEN el documento XML contiene atributos opcionales con valor, THE Parser_XML SHALL poblar los campos correspondientes en el modelo de dominio preservando el valor textual original sin pérdida ni transformación.

### Requerimiento 10: Cadena original para sellado

**Historia de usuario:** Como sistema emisor de facturas, quiero generar la cadena original del CFDI, para que pueda ser firmada digitalmente con el certificado del emisor.

#### Criterios de aceptación

1. WHEN se genera un CFDI, THE Generador_CFDI SHALL producir la cadena original aplicando la transformación XSLT oficial del SAT (`cadenaoriginal_4_0.xslt`) al documento XML serializado en UTF-8.
2. THE Generador_CFDI SHALL generar la cadena original con el formato de campos separados por el carácter pipe (`|`), iniciando y terminando con `||`.
3. THE Validador_CFDI SHALL verificar que la cadena original no contenga retornos de carro (CR), tabuladores (TAB) ni espacios múltiples consecutivos.
4. IF la cadena original contiene retornos de carro, tabuladores o espacios múltiples consecutivos, THEN THE Validador_CFDI SHALL rechazar la generación indicando que la cadena original contiene caracteres de espaciado no permitidos.
5. THE Generador_CFDI SHALL generar el sello digital (atributo `Sello`) aplicando el algoritmo SHA-256 con RSA sobre la cadena original utilizando la llave privada del emisor, y codificando el resultado en Base64.
6. IF la transformación XSLT falla o no produce una cadena original válida, THEN THE Generador_CFDI SHALL rechazar la generación indicando que no fue posible obtener la cadena original del comprobante.

### Requerimiento 11: Validación de catálogos del SAT

**Historia de usuario:** Como sistema emisor de facturas, quiero validar que las claves utilizadas en el CFDI pertenezcan a los catálogos oficiales del SAT, para asegurar que el comprobante sea aceptado por el PAC.

#### Criterios de aceptación

1. THE Validador_CFDI SHALL verificar todas las claves de catálogo del CFDI contra el catálogo vigente del SAT correspondiente antes de permitir la generación del XML, evaluando la totalidad de las claves sin detenerse en la primera falla.
2. WHEN una o más claves de catálogo no existen en el catálogo vigente del SAT, THE Validador_CFDI SHALL rechazar la generación e incluir en la respuesta de error la lista completa de fallas, indicando para cada una: el valor de la clave inválida, el nombre del catálogo contra el que se validó y el campo del CFDI donde se encontró.
3. THE Validador_CFDI SHALL validar los siguientes catálogos: c_FormaPago, c_Moneda, c_TipoDeComprobante, c_MetodoPago, c_RegimenFiscal, c_UsoCFDI, c_ClaveProdServ, c_ClaveUnidad, c_Impuesto, c_TipoFactor, c_ObjetoImp y c_Exportacion.
4. IF un catálogo contiene fechas de vigencia, THEN THE Validador_CFDI SHALL verificar que la fecha de emisión del comprobante sea mayor o igual a la fecha de inicio de vigencia de la clave y menor o igual a la fecha de fin de vigencia de la clave; si la fecha de inicio de vigencia no está definida, la clave se considera válida desde cualquier fecha anterior; si la fecha de fin de vigencia no está definida, la clave se considera válida indefinidamente.
5. IF el Validador_CFDI no puede acceder a los datos de un catálogo del SAT durante la validación, THEN THE Validador_CFDI SHALL rechazar la generación del CFDI indicando que la validación no pudo completarse por indisponibilidad del catálogo afectado.
