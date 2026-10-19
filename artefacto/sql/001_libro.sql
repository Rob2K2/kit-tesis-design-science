IF DB_ID(N'TransferenciasLab') IS NULL
    CREATE DATABASE TransferenciasLab;
GO
USE TransferenciasLab;
GO

IF OBJECT_ID(N'dbo.Cuenta', N'U') IS NULL
CREATE TABLE dbo.Cuenta (
    CuentaId nvarchar(40) NOT NULL PRIMARY KEY,
    Saldo decimal(18, 2) NOT NULL
);

IF OBJECT_ID(N'dbo.Transferencia', N'U') IS NULL
CREATE TABLE dbo.Transferencia (
    TransferenciaId uniqueidentifier NOT NULL PRIMARY KEY,
    CuentaId nvarchar(40) NOT NULL,
    Monto decimal(18, 2) NOT NULL,
    Estado nvarchar(40) NOT NULL,
    CreadaEn datetime2 NOT NULL,
    ActualizadaEn datetime2 NOT NULL,
    PublicadorConfirmadoEn datetime2 NULL,
    ConfirmacionInicialEn datetime2 NULL
);

IF OBJECT_ID(N'dbo.Retencion', N'U') IS NULL
CREATE TABLE dbo.Retencion (
    TransferenciaId uniqueidentifier NOT NULL PRIMARY KEY,
    Monto decimal(18, 2) NOT NULL,
    Vigente bit NOT NULL
);

IF OBJECT_ID(N'dbo.TrabajoPendiente', N'U') IS NULL
CREATE TABLE dbo.TrabajoPendiente (
    TransferenciaId uniqueidentifier NOT NULL PRIMARY KEY,
    Carga nvarchar(max) NOT NULL,
    Estado nvarchar(20) NOT NULL
);

IF OBJECT_ID(N'dbo.IdentidadProcesada', N'U') IS NULL
CREATE TABLE dbo.IdentidadProcesada (
    TransferenciaId uniqueidentifier NOT NULL PRIMARY KEY
);

IF OBJECT_ID(N'dbo.Efecto', N'U') IS NULL
CREATE TABLE dbo.Efecto (
    EfectoId uniqueidentifier NOT NULL PRIMARY KEY,
    TransferenciaId uniqueidentifier NOT NULL,
    Tipo nvarchar(20) NOT NULL,
    Monto decimal(18, 2) NOT NULL
);

IF OBJECT_ID(N'dbo.Ventana', N'U') IS NULL
CREATE TABLE dbo.Ventana (
    Id int NOT NULL PRIMARY KEY CHECK (Id = 1),
    Abierta bit NOT NULL,
    ReabiertaEn datetime2 NULL
);

IF OBJECT_ID(N'dbo.Ejecucion', N'U') IS NULL
CREATE TABLE dbo.Ejecucion (
    EjecucionId uniqueidentifier NOT NULL PRIMARY KEY,
    Variante nvarchar(40) NOT NULL,
    Escenario nvarchar(40) NOT NULL,
    Repeticion int NOT NULL,
    IniciadaEn datetime2 NOT NULL
);

IF OBJECT_ID(N'dbo.SolicitudEntrante', N'U') IS NULL
CREATE TABLE dbo.SolicitudEntrante (
    SolicitudId uniqueidentifier NOT NULL PRIMARY KEY,
    CuentaId nvarchar(40) NOT NULL,
    Monto decimal(18, 2) NOT NULL,
    Procesada bit NOT NULL
);

IF OBJECT_ID(N'dbo.CamaraSolicitud', N'U') IS NULL
CREATE TABLE dbo.CamaraSolicitud (
    TransferenciaId uniqueidentifier NOT NULL PRIMARY KEY,
    CuentaId nvarchar(40) NOT NULL,
    Monto decimal(18, 2) NOT NULL,
    RecibidaEn datetime2 NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM dbo.Ventana)
    INSERT INTO dbo.Ventana (Id, Abierta, ReabiertaEn) VALUES (1, 1, NULL);

IF COL_LENGTH(N'dbo.Transferencia', N'DuracionAceptacionMs') IS NULL
    ALTER TABLE dbo.Transferencia ADD DuracionAceptacionMs int NULL;
