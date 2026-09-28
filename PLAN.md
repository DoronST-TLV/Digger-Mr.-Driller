# Strata — תוכנית עבודה לסיום (v2, 28.9.2026)

הגשה: **שבת 4.10.2026**. נשארו 6 ימים. אנחנו (דורון + אביב) עושים הכל ביחד, לפי הסדר הזה. אין פיצול משימות.

---

## 1. איך אנחנו עובדים

1. **Claude (הצ'אט)** הוא המפתח: כותב את הקוד, האסטים, ההוראות.
2. **Claude Code** (ב-VS Code) מבצע במחשב: git, הרצת Unity ב-batch, קומפילציה, תיקוני שגיאות, בילדים. ההוראות שלו ב-`CLAUDE.md`.
3. **אנחנו** לוחצים Play, משחקים, ומדווחים לצ'אט: מה קרה, מה אדום ב-Console, מה מרגיש לא טוב.
4. כל תיקון = קומיט קטן עם הודעה שאומרת מה תוקן.

הפרומפט ל-Claude Code בכל סבב: `Read CLAUDE.md and do the tasks in order. Stop and ask me only if something blocks you.`

**הקוד כבר כתוב.** הימים הבאים הם התקנה, בדיקה, תיקון, בילד, הגשה — לא פיתוח מאפס.

---

## 2. לוח זמנים

| יום | מטרה | סיימנו כש… |
|---|---|---|
| ב' 28.9 | הקמה + Play ראשון | Unity מותקן, החבילה בפרויקט, Claude Code סיים קומפילציה + SceneBuilder, ה-Play מריץ Title → חפירה → קסקדה |
| ג' 29.9 | פלייטסט + באגים + טיונינג | 20 ריצות ברצף בלי אדום ב-Console; `GameConfig` מכוון (קצב אוויר, fallDelay, סיכוי X) כך ש-2–3 דקות הן ריצה טובה |
| ד' 30.9 | בילדים + Device Simulator + **הקפאת פיצ'רים** | `Strata.exe` רץ מתיקייה נקייה; APK נבנה בלי שגיאות; טאץ', portrait ו-safe area נבדקו ב-Device Simulator ב-3 מכשירים; אם השגנו טלפון אנדרואיד להשאלה — ה-APK מותקן ורץ; שום פיצ'ר חדש מכאן |
| ה' 1.10 | סאניטי + קריאות + מסמכים | כל סעיף 4 מסומן; כל סעיף 5 מסומן; README מלא; changelog ב-GDD |
| ו' 2.10 | הגשה | GitHub Release v1.0 (exe zip + apk); קישור הריפו + הבילדים ב-MAMA |
| ש' 3–4.10 | באפר | — |

---

## 3. מה יש בחבילה (`Assets/_Project`)

| קובץ | תפקיד | דפוס |
|---|---|---|
| `Scripts/GameConfig.cs` | כל הפרמטרים | ScriptableObject |
| `Scripts/GridManager.cs` | נתוני הגריד, יצירת שורות, מחזור, חפירה, קסקדה נפילה→מיזוג | coroutine, events, Gizmos |
| `Scripts/Block.cs`, `BlockPool.cs` | תצוגת בלוק + פול | `ObjectPool<Block>` |
| `Scripts/LevelGenerator.cs` | שורה חדשה לפי עומק | — |
| `Scripts/PlayerController.cs`, `InputHandler.cs` | החופר, באפר קלט, טאפ יחסית לשחקן | coroutine |
| `Scripts/AirMeter.cs` | אוויר, קפסולות, X | events |
| `Scripts/GameManager.cs` | מכונת מצבים, ניקוד, Best | singleton, PlayerPrefs |
| `Scripts/UIManager.cs`, `SafeArea.cs` | Title / HUD / Pause / Game Over | events |
| `Scripts/AudioManager.cs`, `BurstPool.cs`, `JuiceController.cs`, `ScreenShake.cs` | סאונד, חלקיקים, שייק | singleton (persistent), `ObjectPool<ParticleSystem>` |
| `Scripts/CameraFollow.cs` | מצלמה יורדת בלבד, ממלאה רוחב | — |
| `Editor/SceneBuilder.cs` | בונה Config + Prefabs + `Game.unity` מקוד | — |
| `Editor/BuildScript.cs` | Build Windows / Android מהתפריט או מ-batch | — |
| `Art/*.png`, `Audio/*.wav` | אמנות וסאונד שלנו | — |

סטיות מה-GDD שנרשום ב-changelog: UI ב-legacy `Text` במקום TMP (יציב לבנייה מקוד); `sortingOrder` במקום Sorting Layers; ספרייטים משלנו במקום Kenney; Bilinear במקום Point.

---

## 4. רשימת סאניטי — הלקח מה-Flappy (40% מהציון)

כל פריט נבדק ומסומן. בלי "בטח שזה עובד".

**גבולות**
- [ ] שמאלה בעמודה 0 / ימינה בעמודה 8 — לא קורה כלום, בלי שגיאה, בלי אנימציה
- [ ] אי אפשר לעלות מעל שורת ההתחלה; אין חפירה למעלה
- [ ] המצלמה אף פעם לא עולה, ואף פעם לא מראה תא שלא נוצר (בדיקה: `bufferRows` = 12, לרוץ למטה מהר)
- [ ] השחקן אף פעם לא בין תאים אחרי שהתנועה נגמרה
- [ ] השחקן אף פעם לא בתוך בלוק (גם אחרי נפילה, גם אחרי מיזוג מתחתיו)
- [ ] בלוק אף פעם לא נופל **בלי** שייק לפני
- [ ] בלוק אף פעם לא נופל הצידה, ולא נעצר באוויר

**לולאה**
- [ ] 20 ריצות רצופות עם restart — אין שגיאות, מספר הבלוקים ב-Hierarchy זהה בכל ריצה
- [ ] מוות ממעיכה ומוות מאוויר — שניהם מציגים את הסיבה הנכונה
- [ ] בזמן lockout טאפ לא עושה כלום; אחריו — restart
- [ ] שרשרת: המונה עולה, מתאפס כשהגריד נח, הניקוד מוכפל נכון
- [ ] קבוצה גדולה שנפלה שלמה **לא** נמחקת; קבוצה שהתמזגה ל-4+ — כן
- [ ] X-block: עולה אוויר, לא מצטרף לקבוצות, נופל לבד; חפירת X עם אוויר ≤ 20% = מוות מאוויר
- [ ] קפסולה: +20, לא עובר 100
- [ ] אוויר לא יורד לפני הקלט הראשון, ולא יורד ב-Title / Pause / Game Over
- [ ] הקסקדה תמיד מסתיימת — בדיקה: 5 דקות משחק אגרסיבי
- [ ] נפילת שחקן ונפילת קבוצה על אותו תא באותו רגע — מוות, לא קריסה

**UI וקלט**
- [ ] טאפ על כפתור UI לא חופר
- [ ] טאפ מעל השחקן לא עושה כלום
- [ ] קלט באמצע tween נשמר ומתבצע פעם אחת, לא פעמיים
- [ ] Best מתעדכן רק כשנשבר; שורד סגירה ופתיחה של האפליקציה
- [ ] Pause עוצר אוויר וקסקדה; Resume ממשיך; Pause פעמיים לא שובר כלום
- [ ] "NEW BEST" מופיע רק כשבאמת חדש

**מובייל**
- [ ] Portrait נעול, אין סיבוב
- [ ] Back = Pause, לא יציאה
- [ ] מעבר לאפליקציה אחרת וחזרה — המשחק ב-Pause, לא מת
- [ ] 3 יחסי מסך ב-Device Simulator — הגריד ממלא רוחב, HUD בתוך safe area, כלום לא נחתך
- [ ] 60 fps ב-Stats על PC

**בילד וריפו**
- [ ] ה-exe רץ מתיקייה נקייה במחשב אחר
- [ ] ה-APK נבנה; אם יש טלפון להשאלה — מותקן ורץ (אין לנו אנדרואיד, הבדיקה העיקרית היא Device Simulator)
- [ ] `git status` נקי; אין `Library`, אין `Builds`, אין `.claude`, אין `CLAUDE.md` בריפו
- [ ] Console נקי מאדום **וגם מצהוב** ב-Play מלא: Title → משחק → מוות → restart
- [ ] GDD.md, README.md, `.gitignore` בשורש; שתי התמונות נטענות ב-GitHub

---

## 5. רשימת קריאות קוד (20% מהציון)

- [ ] שם קובץ = שם מחלקה; מחלקה אחת לקובץ; namespace `Strata`
- [ ] `[SerializeField] private` — אף שדה `public` חוץ מ-`GameConfig` (נכס נתונים), אירועים ו-properties
- [ ] אפס מספרים קשיחים בלוגיקה — הכל מ-`GameConfig`
- [ ] כל מחלקה עושה דבר אחד
- [ ] מתודות עד ~30 שורות; הקסקדה מפוצלת ל-`FindUnsupportedGroups / MoveCellDown / FindMergedGroups`
- [ ] הערה אחת בראש כל מחלקה; הערות בגוף רק במקומות לא מובנים מאליהם (חוק המיזוג, חוק התמיכה)
- [ ] אפס `Debug.Log` שנשארו; אפס קוד מוער; אפס `using` לא בשימוש
- [ ] `FindObjectOfType` — אפס; הכל דרך singletons או `[SerializeField]`
- [ ] שמות אירועים `On...`, קורוטינות `...Routine`, booleans `is/has/can...`
- [ ] Inspector של כל פריפאב וכל אובייקט בסצנה: אין `None (Missing)`

---

## 6. שימוש חוזר — מה לא כתבנו מאפס

| מה | מאיפה | רישיון |
|---|---|---|
| Object pool | `UnityEngine.Pool.ObjectPool<T>` מובנה | — |
| UI, CanvasScaler, Device Simulator, ParticleSystem | מובנה ב-Unity | — |
| דפוס Singleton / UIManager | פרויקט ה-Flappy Bird שלנו | שלנו |
| אלגוריתם נפילה/מיזוג — **לקריאה בלבד** | [QuentinGruber/Mr.Driller-Clone](https://github.com/QuentinGruber/Mr.Driller-Clone) | MIT |
| ארגון סצנה — **לא להעתיק** | [hadashiA/Mrs.Driller](https://github.com/hadashiA/Mrs.Driller) | אין רישיון |

אסטים: כולם שלנו (נוצרו בקוד). אם יישאר זמן אחרי ההקפאה — אפשר להחליף ל-[Kenney Puzzle Pack 2](https://opengameart.org/content/puzzle-pack-2-795-assets) (CC0), אבל זה לא חובה.
