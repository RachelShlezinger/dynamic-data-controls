# מערכת בקרות נתונים דינמית

מערכת Web גנרית לניהול בקרות איכות נתונים ללא כתיבת SQL.
טכנולוגיות: .NET 8, Angular 17, Python FastAPI, SQL Server LocalDB.

---

## הוראות התקנה והרצה

### דרישות מוקדמות

| כלי | גרסה |
|-----|-------|
| Visual Studio 2022 / VS Code | כולל LocalDB |
| .NET SDK | 8.0 |
| Node.js | 20 LTS |
| Python | 3.11+ |
| Git | כלשהי |

LocalDB מגיע עם Visual Studio - אין צורך ב-SQL Server נפרד.

---

### שלב 1 - הורד את הפרויקט

```bash
git clone https://github.com/YOUR_NAME/dynamic-data-controls.git
cd dynamic-data-controls
```

---

### שלב 2 - הקמת מסד הנתונים

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE DynamicDataControls_System"
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "CREATE DATABASE DynamicDataControls_Target"
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DynamicDataControls_Target -i db/target-schema.sql
sqlcmd -S "(localdb)\MSSQLLocalDB" -d DynamicDataControls_Target -i db/seed-data.sql
```

> טבלאות System DB נוצרות אוטומטית בסטארט הראשון. אין צורך ב-system-schema.sql.

---

### שלב 3 - .NET API (טרמינל 1)

```powershell
cd backend/DynamicDataControls.Api
dotnet build
dotnet run
```

תוצאה מצופה:
```
info: Ensuring database schema exists...
info: 4 demo controls seeded.
info: Now listening on: http://localhost:5000
```

---

### שלב 4 - Python AI Service (טרמינל 2)

```powershell
cd ai-service
python -m venv .venv
.venv\Scripts\Activate.ps1
pip install -r requirements.txt
uvicorn main:app --reload
```

בדיקה: http://localhost:8000/health

---

### שלב 5 - Angular (טרמינל 3)

```powershell
cd frontend
npm install
npm start
```

פתח דפדפן: **http://localhost:4200**

---

### שלב 6 - JWT לפעולות כתיבה

```powershell
Invoke-RestMethod -Method POST http://localhost:5000/api/auth/token `
  -ContentType "application/json" `
  -Body '{"username":"admin","password":"test","role":"manager"}'
```

---

### שלב 7 - הרצה ראשונה

1. פתח http://localhost:4200
2. תראה 4 בקרות מוגדרות מראש
3. לחץ **הרץ** על 'עובדים קטינים עם שכר גבוה'
4. תוצאה: 3 רשומות חריגות (עובדים מתחת לגיל 18 עם שכר גבוה)

---

### שלב 8 - הדגמת שלב ב' AI

1. עבור לעמוד **ממשק AI** בתפריט
2. כתוב: 'מצא עובדים קטינים עם שכר גבוה'
3. לחץ **נתח תיאור**
4. המערכת מציגה הצעת בקרה לאישור
5. לחץ **אשר ושמור**

---

## ארכיטקטורה

```
Angular 17 RTL/Hebrew
       |
  REST+JSON
       v
.NET 8 Web API
  - ControlsController
  - MetadataController
  - ExecutionController
  - AiProxyController
       |             |
  EF Core       HTTP internal
       v             v
  SQL Server     Python FastAPI
  SystemDB + TargetDB   (MockAI)
```

### שכבות

| שכבה | תיאור |
|------|--------|
| Controllers | HTTP endpoints |
| Services | לוגיקה עסקית |
| QueryBuilderService | JSON -> SQL דינמי |
| ExecutionService | ADO.NET על Target DB |
| AiProxyService | Context_AI + Python |
| Repositories | EF Core |

### מנגנון הבקרות

1. הגדרה - ישות + שדות + תנאים ללא SQL
2. שמירה - DefinitionJson ב-Controls
3. בנייה - JSON -> Parameterized SELECT
4. הרצה - Target DB -> רשומות חריגות
5. תוצאות - טבלה + מיון + חיפוש + CSV

---

## שלב ב' - תשתית AI לשפה חופשית

```
משתמש -> Angular -> .NET AiProxyService
                    -> Python MockAI
                    -> ControlDefinition JSON
                    -> אישור -> שמירה
```

**MockAI מילות מפתח:**

| מפתח | בקרה |
|------|------|
| קטין | עובדים קטינים עם שכר גבוה |
| תאריך_סיום | תאריך סיום מוקדם |
| מחלקה_לא_פעילה | עובדים פעילים במחלקה לא פעילה |
| חסר_נתונים | רשומות עם נתונים חסרים |

**פורמט JSON:**
```json
{
  "controlName": "עובדים קטינים עם שכר גבוה",
  "primaryEntity": "עובד",
  "conditions": [
    { "fieldName": "גיל", "operator": "LessThan", "value": 18 },
    { "fieldName": "שכר_חודשי", "operator": "GreaterThan", "value": 10000 }
  ]
}
```

**החלפה ל-AI אמיתי:** מחליפים רק mock_ai.py - שאר הקוד לא משתנה.

---

## דוגמאות לבקרות

| בקרה | תנאים | אנומליות |
|------|--------|---------|
| עובדים קטינים עם שכר גבוה | גיל < 18 AND שכר > 10000 | 3 |
| תאריך סיום מוקדם | end_date < start_date | 2 |
| עובדים פעילים במחלקה לא פעילה | JOIN + is_active | 5 |
| עובדים ללא שם | שם IS NULL | תלוי |

---

## הנחות עבודה

- LocalDB - מגיע עם Visual Studio ללא SQL Server נפרד
- MockAI - מוכן לחיבור GPT/Claude על ידי החלפת mock_ai.py
- JWT - endpoint ייעודי לפיתוח מקומי
- Target DB - עובדים/מחלקות/חוזים; ניתן להחליף לכל סכמה

## מגבלות ידועות

- תוצאות הרצה נשמרות בזיכרון בלבד
- MockAI תומך ב-4 מילות מפתח
- מקסימום 10,000 רשומות להרצה

## הרחבות אפשריות

- חיבור OpenAI/Azure AI לניתוח שפה חופשית
- שמירת תוצאות ב-DB
- תזמון אוטומטי
- ניהול משתמשים מלא

---

## כלי AI שנעשה בהם שימוש

| כלי | מטרת השימוש |
|-----|-------------|
| **Kiro (Claude Sonnet)** | כתיבת כל קוד המערכת: C#, Angular, Python, SQL |
| **Kiro SPEC** | ניתוח דרישות, מסמך תכנון, יצירת רשימת משימות |
| **MockAI Python** | שלב ב' - מדמה תשובות AI ללא שירות חיצוני |

---

## מבנה התיקיות

```
backend/     - .NET 8 Web API (C#)
frontend/    - Angular 17
ai-service/  - Python FastAPI
db/          - SQL scripts
```
