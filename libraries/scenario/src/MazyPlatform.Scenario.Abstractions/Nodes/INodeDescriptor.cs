namespace MazyPlatform.Scenario.Abstractions.Nodes;

using System.Text.Json;

/// <summary>
/// Полный дескриптор узла: метаданные + фабрика создания экземпляра.
/// Используется в полной сборке сценариев (исполнитель + сборщик).
/// </summary>
/// <remarks>
/// Реализации обязаны быть stateless и регистрируются как singleton.
/// Создаваемые методом <see cref="Create"/> экземпляры узлов после
/// конструирования считаются immutable — всё рантайм-состояние хранится
/// в ExecutionContext и ISession, а не в полях узла.
/// <para>
/// <b>Соглашения о неймингах:</b>
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <b>Type узла</b> — snake_case (например, <c>"send_message"</c>,
///       <c>"vk_send_keyboard"</c>). Префикс платформы перед основным именем,
///       если узел платформо-специфичный.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Ключи параметров в Schema</b> — camelCase (например,
///       <c>"messageIdVariable"</c>, <c>"oneTime"</c>, <c>"photoId"</c>).
///       Snake_case ключи внешних API не пробрасываются наружу — это
///       внутренний контракт между бэком и фронтом, а не контракт стороннего API.
///     </description>
///   </item>
///   <item>
///     <description>
///       Если параметр хранит <b>имя переменной сессии</b>, и дескриптор
///       <b>знает заранее</b>, что туда сохраняется или оттуда читается, —
///       ключ обязан оканчиваться на <c>Variable</c> и отражать <b>семантику</b>
///       значения, а не роль (result/output). Примеры:
///       <c>messageIdVariable</c>, <c>messageTextVariable</c>,
///       <c>buttonPayloadVariable</c>, <c>recordIdVariable</c>,
///       <c>recordsVariable</c>, <c>responseBodyVariable</c>,
///       <c>responseStatusVariable</c>. Общие имена вроде
///       <c>resultVariable</c> не допускаются — они скрывают семантику от
///       пользователя редактора.
///       <para>
///       <b>Исключение:</b> для generic-узлов, где тип значения определяет сам
///       пользователь (<c>set_variable</c>, <c>switch</c>), допустимо короткое
///       имя <c>variable</c> без суффикса — у дескриптора нет знания о
///       семантике содержимого, и навешивать псевдо-семантический префикс
///       (<c>nameVariable</c>, <c>inputVariable</c>) только увеличивает имя
///       без добавления информации.
///       </para>
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Системные переменные сессии.</b> Executor автоматически
///       устанавливает в <c>Session.Variables</c> несколько переменных с
///       префиксом <c>"_"</c>, доступных во всех шаблонах и параметрах узлов:
///       <list type="bullet">
///         <item><description><c>_chatId</c> — идентификатор чата на платформе мессенджера (из <c>IncomingEvent.ChatId</c>).</description></item>
///         <item><description><c>_userId</c> — идентификатор пользователя на платформе (из <c>IncomingEvent.PlatformUserId</c>).</description></item>
///         <item><description><c>_botId</c> — идентификатор бота (из <c>Session.BotId</c>).</description></item>
///       </list>
///       Системные переменные восстанавливаются после каждой очистки
///       <c>Variables</c> при переходе на entry-point или старт сценария —
///       шаблон <c>{_chatId}</c> в первом узле после такого перехода
///       резолвится корректно. Пользовательские переменные не должны
///       начинаться с <c>"_"</c> — этот префикс зарезервирован для будущих
///       системных расширений.
///     </description>
///   </item>
///   <item>
///     <description>
///       Повторяющиеся вложенные структуры (кнопки сообщений, карточки и т.п.)
///       определяются один раз в общей точке (см.
///       <c>MazyPlatform.Scenario.Helpers.ButtonSchemas</c>) и переиспользуются
///       всеми дескрипторами через <see cref="NodeParamSchema.Fields"/>. Не
///       дублируй определения полей вложенных объектов в каждом дескрипторе —
///       это разъезжается со временем и создаёт несогласованность UI.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Условная обязательность параметра</b>: если параметр обязателен
///       только в комбинации с другим (например, <c>errorMessage</c> в
///       <c>receive_message</c> обязателен только при выбранном <c>validatorType</c>),
///       это описывается в <see cref="NodeParamSchema.Description"/> человеческим
///       языком («Обязательно, если выбран validatorType»), а сама проверка
///       выполняется в перегруженном <c>Validate(...)</c> дескриптора.
///       На уровне <see cref="NodeParamSchema"/> сейчас условная обязательность
///       не выражается — <see cref="NodeParamSchema.IsRequired"/> остаётся
///       <c>false</c>. При появлении нескольких узлов с такой логикой стоит
///       рассмотреть введение декларативного механизма (например, поля
///       <c>RequiredWhen</c>) — пока единичный случай решается описанием.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Сайд-эффекты на уровне графа сценария</b>: если параметр узла
///       влияет не только на исполнение самого узла, но и на структуру или
///       жизненный цикл графа (делает узел точкой входа, очищает переменные
///       сессии, прерывает <c>WaitingForEvent</c>-состояние и т.п.), это
///       обязано быть явно описано в трёх местах:
///       (а) в <see cref="NodeParamSchema.Description"/> — для пользователя
///       редактора, на естественном языке;
///       (б) в XML-комментарии соответствующего свойства Node-класса (или
///       параметра конструктора) — для разработчика;
///       (в) при необходимости — со ссылками <c>see cref</c> на места в
///       <c>ScenarioBuilder</c>/<c>ScenarioExecutor</c>, где сайд-эффект
///       реализуется фактически.
///       Пример: <c>ReceiveButtonPressNode.IsEntry</c> — флаг очищает
///       переменные сессии при срабатывании entry-маршрута; это
///       зафиксировано и в Description, и в XML-доке свойства, и в XML-доке
///       <c>NavigateToEntryOrStart</c>.
///     </description>
///   </item>
///   <item>
///     <description>
///       <b>Выбор типа параметра</b>: используй <see cref="NodeParamType.Enum"/>
///       для параметров с фиксированным набором значений (HTTP-методы, цвета,
///       режимы валидации) вместо <see cref="NodeParamType.String"/> — это даёт
///       фронту закрытый список и ловит опечатки на валидации сценария.
///       Используй <see cref="NodeParamType.ObjectList"/> со схемой
///       <see cref="NodeParamSchema.Fields"/> для структурированных списков,
///       где каждый элемент имеет фиксированный набор полей (кейсы Switch,
///       кнопки, карточки), вместо <see cref="NodeParamType.StringDictionary"/>.
///       <see cref="NodeParamType.StringDictionary"/> оставляем для свободных
///       словарей, где набор ключей задаёт сам пользователь (HTTP-заголовки,
///       произвольный фильтр поиска, мапа полей записи).
///     </description>
///   </item>
/// </list>
/// </remarks>
public interface INodeDescriptor : INodeSchemaProvider
{
    /// <summary>
    /// Создаёт экземпляр узла из JSON-параметров.
    /// </summary>
    /// <param name="id">Идентификатор узла.</param>
    /// <param name="parameters">JSON-параметры узла.</param>
    /// <param name="services">Провайдер сервисов для разрешения зависимостей.</param>
    /// <returns>Экземпляр узла.</returns>
    INode Create(Guid id, JsonElement parameters, IServiceProvider services);
}
