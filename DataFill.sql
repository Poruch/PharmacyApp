BEGIN TRANSACTION;

-- 1. Пользователи (если есть, но в вашем фрагменте их нет – добавлю для полноты, если нужны)
-- ... (я их пропущу, так как не в вашем фрагменте)

-- 2. Категории
SET IDENTITY_INSERT CATEGORY ON;
INSERT INTO CATEGORY (Id, Name, Description, ParentId)
VALUES 
(1, N'Лекарственные средства', N'Препараты, отпускаемые по рецепту и без', NULL),
(2, N'Анальгетики', N'Обезболивающие препараты', 1),
(3, N'Антибиотики', N'Противомикробные средства', 1),
(4, N'Медицинские изделия', N'Перевязочные материалы, инструменты', NULL),
(5, N'Парафармацевтика', N'Косметика, БАДы, гигиена', NULL);
SET IDENTITY_INSERT CATEGORY OFF;

-- 3. Поставщики
SET IDENTITY_INSERT SUPPLIER ON;
INSERT INTO SUPPLIER (Id, Name, INN, Phone, Email)
VALUES 
(1, N'ФармДистрибут', N'770123456789', N'+7(495)123-45-67', N'order@farmdist.ru'),
(2, N'МедТехСервис', N'771234567890', N'+7(812)234-56-78', N'sales@medtech.ru'),
(3, N'Аптечный Склад №1', N'772345678901', N'+7(343)345-67-89', N'zakaz@aptechka.ru');
SET IDENTITY_INSERT SUPPLIER OFF;

-- 4. Товары
SET IDENTITY_INSERT ITEM ON;
INSERT INTO ITEM (Id, Name, INN, Dosage, Form, PrescriptionRequired, Barcode, MinStock, IsVital, TempMin, TempMax, QuantityPerPack, Unit, CategoryId)
VALUES 
(1, N'Парацетамол 500 мг', N'парацетамол', N'500 мг', N'таблетки', 0, N'4601234567890', 100, 0, 15, 25, 20, N'шт', 2),
(2, N'Нурофен 200 мг', N'ибупрофен', N'200 мг', N'таблетки', 0, N'4601234567891', 50, 0, 15, 25, 10, N'шт', 2),
(3, N'Амоксициллин 500 мг', N'амоксициллин', N'500 мг', N'капсулы', 1, N'4601234567892', 30, 1, 2, 8, 20, N'шт', 3),
(4, N'Бинт стерильный 5мх10см', NULL, NULL, N'рулон', 0, N'4601234567893', 200, 0, 10, 30, 1, N'шт', 4),
(5, N'Витамин С шипучий', N'аскорбиновая кислота', N'1000 мг', N'таблетки шипучие', 0, N'4601234567894', 80, 0, 15, 25, 15, N'шт', 5);
SET IDENTITY_INSERT ITEM OFF;

-- 5. Партии
SET IDENTITY_INSERT BATCH ON;
INSERT INTO BATCH (Id, BatchNumber, ProductionDate, ExpiryDate, PurchasePrice, RetailPrice, Quantity, StorageLocation, ItemId, SupplierId)
VALUES 
(1, N'BATCH001', '2025-01-10', '2026-01-10', 100.00, 180.00, 200, N'Стеллаж А полка 2', 1, 1),
(2, N'BATCH002', '2025-02-15', '2026-02-15', 150.00, 280.00, 150, N'Стеллаж А полка 1', 2, 1),
(3, N'BATCH003', '2025-01-20', '2025-08-20', 80.00, 140.00, 50, N'Холодильник №1 полка 1', 3, 2),
(4, N'BATCH004', '2025-03-01', '2027-03-01', 20.00, 45.00, 500, N'Стеллаж Б полка 1', 4, 3),
(5, N'BATCH005', '2025-02-10', '2026-02-10', 250.00, 450.00, 120, N'Стеллаж А полка 2', 5, 1);
SET IDENTITY_INSERT BATCH OFF;

-- 6. Маркированные экземпляры (коды маркировки – обычно латиница, но для единообразия и если могут быть кириллические коды)
SET IDENTITY_INSERT UNIQUE_ITEM ON;
DECLARE @i INT = 1;
WHILE @i <= 50
BEGIN
    INSERT INTO UNIQUE_ITEM (Id, QrCode, Status, BatchId)
    VALUES (@i, N'CODE' + RIGHT('000' + CAST(@i AS NVARCHAR), 3), N'in_stock', 3);
    SET @i = @i + 1;
END
SET IDENTITY_INSERT UNIQUE_ITEM OFF;

COMMIT TRANSACTION;