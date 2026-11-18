USE SistemaSolicitudes;
GO

CREATE TABLE Expediente_Recurso (
    -- IDENTIFICADOR PRINCIPAL
    id_expediente INT IDENTITY(1,1) PRIMARY KEY,

    -- ==========================================
    -- BLOQUE A: DATOS GENERALES DEL RECURSO
    -- ==========================================
    numero_recurso NVARCHAR(50) NOT NULL UNIQUE, 
    folio_solicitud NVARCHAR(50) NOT NULL,
    materia_recurso NVARCHAR(100) NULL, 
    nombre_recurrente NVARCHAR(200) NULL,
    fecha_presentacion DATETIME NULL,
    estatus NVARCHAR(100) NULL, 
    responsable_interno NVARCHAR(150) NULL, 

    -- ==========================================
    -- BLOQUE B: DATOS DE LA SOLICITUD ORIGINAL
    -- ==========================================
    contenido_solicitud NVARCHAR(MAX) NULL, 
    fecha_respuesta_solicitud DATETIME NULL,
    respuesta_otorgada NVARCHAR(MAX) NULL,
    razon_interposicion NVARCHAR(500) NULL, 

    -- ==========================================
    -- BLOQUE C: TRÁMITE DEL RECURSO
    -- ==========================================
    fecha_admision DATETIME NULL,
    fecha_notificacion_admision DATETIME NULL,
    fecha_limite_contestacion DATETIME NULL,
    fecha_contestacion_recurso DATETIME NULL,
    sentido_contestacion NVARCHAR(500) NULL,
    fecha_cierre_instruccion DATETIME NULL,
    fecha_resolucion DATETIME NULL,
    sentido_resolucion NVARCHAR(200) NULL, 
    fecha_notificacion_resolucion DATETIME NULL,

    -- ==========================================
    -- BLOQUE D: CUMPLIMIENTO
    -- ==========================================
    requiere_cumplimiento BIT NULL DEFAULT 0, 
    fecha_limite_cumplimiento DATETIME NULL,
    fecha_cumplio DATETIME NULL,
    cumplio_resolucion NVARCHAR(100) NULL, 
    acuerdo_cumplimiento NVARCHAR(MAX) NULL,
    observaciones NVARCHAR(MAX) NULL,

    -- ==========================================
    -- METADATOS DEL SISTEMA
    -- ==========================================
    fecha_creacion DATETIME NOT NULL DEFAULT GETDATE(),
    fecha_modificacion DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Tabla de archivos vinculados
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expediente_Archivos')
BEGIN
    CREATE TABLE Expediente_Archivos (
        id_archivo INT IDENTITY(1,1) PRIMARY KEY,
        id_expediente INT NOT NULL,
        nombre_archivo NVARCHAR(255) NOT NULL,
        contenido VARBINARY(MAX) NOT NULL,
        tipo_documento VARCHAR(100) NULL,
        fecha_documento DATE NULL,
        descripcion VARCHAR(250) NULL,
        usuario_registro VARCHAR(100) NULL DEFAULT 'Usuario',
        fecha_registro DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT fk_Expediente_Archivos FOREIGN KEY (id_expediente)
            REFERENCES Expediente_Recurso(id_expediente)
            ON DELETE CASCADE
    );
END;
GO

-- Tabla Bitacora enlazada a expediente_recurso
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expediente_Bitacora')
BEGIN
    CREATE TABLE Expediente_Bitacora (
        id_actuacion INT IDENTITY(1,1) PRIMARY KEY,
        id_expediente INT NOT NULL,
        fecha_actuacion DATE NOT NULL,
        actuacion NVARCHAR(150) NOT NULL,
        descripcion NVARCHAR(500) NULL,
        documento_vinculado NVARCHAR(255) NULL,
        usuario NVARCHAR(100) NULL DEFAULT 'Usuario',
        fecha_registro DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT fk_Expediente_Bitacora FOREIGN KEY (id_expediente)
            REFERENCES Expediente_Recurso(id_expediente)
            ON DELETE CASCADE
    );
END;
GO


