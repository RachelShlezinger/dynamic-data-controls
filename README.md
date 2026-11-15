# מערכת בקרות נתונים דינמית

מערכת Web גנרית לניהול בקרות איכות נתונים ללא כתיבת קוד.

---

## ארכיטקטורה

`
Angular 17 (RTL / Hebrew)
    |
    v
.NET 8 Web API (C#)
  - ControlsController
  - MetadataController
  - ExecutionController
  - AiProxyController
    |             |
    v             v
SQL Server     Python FastAPI
(LocalDB)      (MockAI)
`

### שכבות

| שכבה | תיאור |
|------|--------|
| Controllers | HTTP endpoints |
| Services | לוגיקה עסקית |
| QueryBuilderService | JSON -> SQL דינמי |
| ExecutionService | הרצת SQL על Target DB |
| AiProxyService | Context_AI + Python |

### מנגנון

1. הגדרה - ישות + שדות + תנאים
2. שמירה - DefinitionJson ב-Controls
3. בנייה - JSON -> SELECT + Parameterized Queries
4. הרצה - Target DB -> רשומות חריגות
5. תוצאות - טבלה + מיון + חיפוש + CSV

---

## שלב ב - תשתית AI

משתמש כותב עברית חופשית -> מקבל הצעת בקרה.

**זרימה:**

`
משתמש -> Angular -> .NET AiProxyService
                    -> Python MockAI
                    -> ControlDefinition JSON
                    -> אישור -> שמירה
`

**MockAI מילות מפתח:**

| מפתח | בקרה |
|------|------|
| קטין | עובדים קטינים עם שכר גבוה |
| תאריך_סיום | תאריך סיום מוקדם |
| מחלקה_לא_פעילה | עובדים פעילים במחלקה לא פעילה |
| חסר_נתונים | רשומות עם נתונים חסרים |

**פורמט JSON:**
`json
{
  "controlName": "...",
  "primaryEntity": "...",
  "conditions": [{ "fieldName": "...", "operator": "...", "value": ... }]
}
`

החלפה ל-AI אמיתי: מחליפים רק mock_ai.py

---

## דוגמאות לבקרות

4 בקרות נטענות אוטומטית:

| בקרה | תנאים | אנומליות |
|------|--------|---------|
| עובדים קטינים עם שכר גבוה | גיל < 18 AND שכר > 10000 | 3 |
| תאריך סיום מוקדם | end_date < start_date | 2 |
| עובדים פעילים במחלקה לא פעילה | JOIN + is_active | 5 |
| עובדים ללא שם | שם IS NULL | תלוי |

---

## הנחות עבודה

- LocalDB - מגיע עם Visual Studio
- MockAI - מוכן לחיבור GPT/Claude
- JWT - endpoint לפיתוח מקומי
- Target DB - עובדים/מחלקות/חוזים

## מגבלות

- תוצאות בזיכרון בלבד
- MockAI - 4 מילות מפתח
- מקסימום 10,000 רשומות

## הרחבות

- OpenAI/Azure AI
- שמירת תוצאות ב-DB
- תזמון אוטומטי

---

## כלי AI שהיה שימוש

| כלי | מטרה |
|-----|------|
| Kiro (Claude Sonnet) | כתיבת כל הקוד: C#, Angular, Python, SQL |
| Kiro SPEC | ניתוח דרישות, תכנון, משימות |
| MockAI Python | שלב ב - ללא שירות חיצוני |

---

## Prerequisites

- Visual Studio + LocalDB
- .NET SDK 8.0
- Node.js 20
- Python 3.11

---

## הרצה מקומית

**DB (פעם אחת):**
`
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE DynamicDataControls_System"
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE DynamicDataControls_Target"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DynamicDataControls_Target -i db/target-schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DynamicDataControls_Target -i db/seed-data.sql
`

**3 טרמינלים:**
`
# 1 - .NET API
cd backend/DynamicDataControls.Api && dotnet run

# 2 - Python AI
cd ai-service && uvicorn main:app --reload

# 3 - Angular
cd frontend && npm start
`

פתחי: http://localhost:4200

---

## מבנה

`
StaticProject/
  backend/   - .NET C#
  frontend/  - Angular 17
  ai-service/ - Python FastAPI
  db/        - SQL scripts
`
