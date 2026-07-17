# -*- coding: utf-8 -*-
"""Fix mojibake quotation seed + Product 4 price. Uses Unicode escapes only."""
from pathlib import Path

def u(*codes):
    return "".join(chr(c) for c in codes)

# Prebuild Vietnamese via codepoints (avoid editor encoding issues)
sofa = "Sofa Nordic 3 ch" + u(0x1ED7)  # chỗ
ban_tra = "B" + u(0x00E0) + "n tr" + u(0x00E0)  # Bàn trà
ke = "K" + u(0x1EC7) + " TV Walnut"  # Kệ
den = u(0x0110, 0x00E8) + "n s" + u(0x00E0) + "n Brass"  # Đèn sàn
giuong = "Gi" + u(0x01B0, 0x1EDD) + "ng Sleepwell 1m6"  # Giường

an = "Nguy" + u(0x1EC5) + "n V" + u(0x0103) + "n An"  # Nguyễn Văn An
binh = "Tr" + u(0x1EA7) + "n Th" + u(0x1ECB) + " B" + u(0x00EC) + "nh"  # Trần Thị Bình

bao_gia = "B" + u(0x00E1) + "o gi" + u(0x00E1) + " "
phong = "ph" + u(0x00F2) + "ng "
title1 = bao_gia + phong + "kh" + u(0x00E1) + "ch 20m2"
title2 = (
    bao_gia + phong + "ng" + u(0x1EE7) + " t" + u(0x1ED1) + "i gi" + u(0x1EA3) + "n"
)  # phòng ngủ tối giản

desc1 = f"Sofa Nordic + {ban_tra} Oak + {ke}"
desc2 = f"{giuong} + {den}"
reply2 = (
    "T" + u(0x1ED5) + "ng 11.900.000 " + u(0x20AB)
    + " theo catalog hi" + u(0x1EC7) + "n t" + u(0x1EA1) + "i."
)
notes1 = f"Sofa 12.5M + {ban_tra} 3.2M + {ke.split()[0]} 6.8M = 22.5M"
notes2 = (
    "Gi" + u(0x01B0, 0x1EDD) + "ng 9.8M + "
    + u(0x0110, 0x00E8) + "n 2.1M = 11.9M - da duyet"
)

def esc(s: str) -> str:
    return s.replace("'", "''")

sql = f"""
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
UPDATE Products SET Price = 6800000, UpdatedAt = SYSUTCDATETIME() WHERE Id = 4;
UPDATE Products SET Name = N'{esc(sofa)}' WHERE Id = 1;
UPDATE Products SET Name = N'{esc(ban_tra + " Oak")}' WHERE Id = 2;
UPDATE Products SET Name = N'{esc(ke)}' WHERE Id = 4;
UPDATE Products SET Name = N'{esc(den)}' WHERE Id = 5;
UPDATE Products SET Name = N'{esc(giuong)}' WHERE Id = 6;
UPDATE Users SET FullName = N'{esc(an)}' WHERE Email = N'an@example.com';
UPDATE Users SET FullName = N'{esc(binh)}' WHERE Email = N'binh@example.com';
UPDATE QuotationRequests SET Title = N'{esc(title1)}', Description = N'{esc(desc1)}', Reply = NULL WHERE Id = 1;
UPDATE QuotationRequests SET Title = N'{esc(title2)}', Description = N'{esc(desc2)}', Reply = N'{esc(reply2)}' WHERE Id = 2;
UPDATE Quotations SET Title = N'{esc(title1)}', Notes = N'{esc(notes1)}' WHERE Id = 1;
UPDATE Quotations SET Title = N'{esc(title2)}', Notes = N'{esc(notes2)}' WHERE Id = 2;
SELECT Id, Title FROM QuotationRequests;
SELECT Id, Name, Price FROM Products WHERE Id IN (1,2,4,5,6);
SELECT Id, FullName FROM Users WHERE Email IN (N'an@example.com', N'binh@example.com');
"""

out = Path(r"d:\PRN232\Project\docs\fix_quotation_unicode.sql")
out.write_text(sql, encoding="utf-16")  # LE + BOM
print("wrote", out)
print("title1=", title1)
print("title2=", title2)
print("sofa=", sofa, "price fix Id4 -> 6800000")
