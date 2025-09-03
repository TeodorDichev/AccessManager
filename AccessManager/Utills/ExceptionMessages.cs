namespace AccessManager.Utills
{
    static public class ExceptionMessages
    {
        public const string InvalidEGN = "Невалидно ЕГН. Трябва да съдържа точно 10 цифри.";
        public const string InvalidPhone = "Невалиден телефонен номер.";
        public const string InvalidUsername = "Невалидно потребителско име.";
        public const string InvalidPassword = "Грешна парола.";
        public const string RequiredField = "Това поле е задължително.";
        public const string MissingDirective = "Трябва да изберете подходяща заповед.";
        public const string InvalidLoginAttempt = "Невалидни данни за вход.";
        public const string LoggedInLogInAttempt = "Моля първо излесте от профила си.";
        public const string InsufficientAuthority = "Тази функционалност изисква по-високи права на достъп.";
        public const string AccessNotFound = "Достъпът не е намерен.";
        public const string DirectiveNotFound = "Заповедта не е намерена.";
        public const string ChooseParentAccess = "Моля изберете родителски достъп.";
        public const string UserNotFound = "Потребителят не е намерен.";
        public const string RevokingAccessFailed = "Неуспешен опит за отнемане на достъп.";
        public const string GrantingAccessFailed = "Неуспешен опит за даване на достъп.";
        public const string InvalidRedirect = "Невалиден адрес за пренасочване.";
        public const string EntityCannotBeDeletedDueToDependencies = "Записът не може да бъде изтрит поради съществуващи зависимости.";
        public const string EntityCannotBeRestoredDueToDeletedDependencies = "Записът не може да бъде възстановен поради изтрити зависимости.";
        public const string DepartmentNotFount = "Дирекцията не е намерена.";
        public const string DepartmentWithNameExists = "Вече съществува дирекция с това име.";
        public const string DirectiveWithNameExists = "Вече съществува заповед с това име.";
        public const string UnitNotFound = "Отделът не е намерен.";
        public const string UnitUserNotFound = "Достъпът на потребителя до отдела не е намерен.";
    }
}
