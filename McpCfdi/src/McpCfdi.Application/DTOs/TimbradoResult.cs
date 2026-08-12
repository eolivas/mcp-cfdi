namespace McpCfdi.Application.DTOs;

public record TimbradoResult(
    string Uuid,
    DateTime FechaTimbrado,
    string SelloSat,
    string NoCertificadoSat,
    string SelloCfd,
    string CfdiTimbradoXml);
