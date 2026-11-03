using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AmeWatches.Api.Data.Entities;
using AmeWatches.Api.Models.Email;
using Microsoft.Extensions.Options;

namespace AmeWatches.Api.Services.Email;

public partial class OrderEmailBuilder
{
    private readonly EmailOptions _options;
    public OrderEmailBuilder(IOptions<EmailOptions> options) => _options = options.Value;

    public EmailMessage CustomerConfirmation(Order order)
    {
        var link = $"{_options.PublicBaseUrl.TrimEnd('/')}/porosia/konfirmim/{order.OrderNo}?token={Uri.EscapeDataString(order.AccessToken)}";
        var payment = PaymentText(order);

        var html = new StringBuilder();
        html.Append(Open($"Faleminderit, {Enc(order.CustomerName)}"));
        html.Append($"<p style=\"margin:0 0 18px;color:#4a463f;line-height:1.7\">Porosia juaj u konfirmua. Do t'ju kontaktojmë në <strong>{Enc(order.Phone)}</strong> për dërgesën.</p>");
        html.Append($"<p style=\"margin:0 0 22px;font-size:13px;color:#6f6a60\">Numri i porosisë<br><strong style=\"font-size:20px;letter-spacing:2px;color:#0c0c0d\">{Enc(order.OrderNo)}</strong></p>");
        html.Append(ItemsTable(order));
        html.Append($"<p style=\"margin:22px 0 4px;font-size:14px\"><strong>Pagesa:</strong> {Enc(payment)}</p>");
        html.Append($"<p style=\"margin:0 0 4px;font-size:14px\"><strong>Dërgesa:</strong> {Enc(order.Address)}, {Enc(order.City)}</p>");
        html.Append("<p style=\"margin:0 0 22px;font-size:14px\"><strong>Garancia:</strong> 1 vit nga amé watches për çdo orë.</p>");
        html.Append($"<p style=\"margin:0 0 26px\"><a href=\"{Enc(link)}\" style=\"display:inline-block;background:#0c0c0d;color:#ffffff;text-decoration:none;padding:13px 26px;border-radius:999px;font-size:13px;letter-spacing:1px\">SHIKO POROSINË</a></p>");
        html.Append($"<p style=\"margin:0;font-size:13px;color:#6f6a60;line-height:1.7\">Pyetje? Na shkruani në Instagram <strong>@ame.watches.ks</strong> ose përgjigjuni këtij email-i ({Enc(_options.FromAddress)}).</p>");
        html.Append(Close());

        var text = new StringBuilder()
            .AppendLine($"Faleminderit, {order.CustomerName}")
            .AppendLine()
            .AppendLine($"Porosia juaj {order.OrderNo} u konfirmua.")
            .AppendLine();
        foreach (var item in order.Items)
            text.AppendLine($"- {item.ProductName} ({item.ModelCode}) x {item.Quantity}: {Money(item.UnitPrice * item.Quantity)}");
        text.AppendLine()
            .AppendLine($"Totali: {Money(order.Total)} (dërgesa falas)")
            .AppendLine($"Pagesa: {payment}")
            .AppendLine($"Dërgesa: {order.Address}, {order.City}")
            .AppendLine("Garancia: 1 vit nga amé watches.")
            .AppendLine()
            .AppendLine($"Shiko porosinë: {link}");

        return new EmailMessage(
            order.Email!, Clean(order.CustomerName),
            Clean($"Konfirmimi i porosisë {order.OrderNo} — amé watches"),
            html.ToString(), text.ToString(), ReplyTo: _options.FromAddress);
    }

    public EmailMessage ShopNotification(Order order)
    {
        var adminLink = $"{_options.PublicBaseUrl.TrimEnd('/')}/admin/porosite/{order.OrderNo}";
        var products = string.Join(", ", order.Items.Select(i => $"{i.ProductName} × {i.Quantity}"));
        var payment = PaymentText(order);

        var html = new StringBuilder();
        html.Append(Open("Porosi e re"));
        html.Append($"<p style=\"margin:0 0 6px;font-size:15px\"><strong>{Enc(order.CustomerName)}</strong> porositi:</p>");
        html.Append(ItemsTable(order));
        html.Append($"<p style=\"margin:22px 0 4px;font-size:14px\"><strong>Numri:</strong> {Enc(order.OrderNo)}</p>");
        html.Append($"<p style=\"margin:0 0 4px;font-size:14px\"><strong>Telefoni:</strong> <a href=\"tel:{Enc(order.Phone)}\">{Enc(order.Phone)}</a></p>");
        if (!string.IsNullOrWhiteSpace(order.Email))
            html.Append($"<p style=\"margin:0 0 4px;font-size:14px\"><strong>Email:</strong> {Enc(order.Email)}</p>");
        html.Append($"<p style=\"margin:0 0 4px;font-size:14px\"><strong>Adresa:</strong> {Enc(order.Address)}, {Enc(order.City)}</p>");
        html.Append($"<p style=\"margin:0 0 22px;font-size:14px\"><strong>Pagesa:</strong> {Enc(payment)}</p>");
        html.Append($"<p style=\"margin:0\"><a href=\"{Enc(adminLink)}\" style=\"display:inline-block;background:#0c0c0d;color:#ffffff;text-decoration:none;padding:13px 26px;border-radius:999px;font-size:13px;letter-spacing:1px\">HAP NË PANEL</a></p>");
        html.Append(Close());

        var text = new StringBuilder()
            .AppendLine($"Porosi e re {order.OrderNo}")
            .AppendLine($"Klienti: {order.CustomerName}")
            .AppendLine($"Telefoni: {order.Phone}")
            .AppendLine($"Email: {order.Email}")
            .AppendLine($"Adresa: {order.Address}, {order.City}")
            .AppendLine($"Pagesa: {payment}")
            .AppendLine();
        foreach (var item in order.Items)
            text.AppendLine($"- {item.ProductName} ({item.ModelCode}) x {item.Quantity}: {Money(item.UnitPrice * item.Quantity)}");
        text.AppendLine().AppendLine($"Totali: {Money(order.Total)}").AppendLine($"Paneli: {adminLink}");

        return new EmailMessage(
            _options.ShopAddress, "amé watches",
            Clean($"Porosi e re {order.OrderNo}: {products} — {order.CustomerName}"),
            html.ToString(), text.ToString(),
            ReplyTo: string.IsNullOrWhiteSpace(order.Email) ? null : order.Email);
    }

    private static string ItemsTable(Order order)
    {
        var rows = new StringBuilder();
        foreach (var item in order.Items)
        {
            rows.Append($"<tr><td style=\"padding:10px 0;border-bottom:1px solid #e4ded1;font-size:14px\">{Enc(item.ProductName)}<br><span style=\"color:#6f6a60;font-size:12px\">{Enc(item.ModelCode)} × {item.Quantity}</span></td>");
            rows.Append($"<td style=\"padding:10px 0;border-bottom:1px solid #e4ded1;font-size:14px;text-align:right;white-space:nowrap\">{Money(item.UnitPrice * item.Quantity)}</td></tr>");
        }

        return $"<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"border-collapse:collapse\">{rows}" +
               $"<tr><td style=\"padding:10px 0;font-size:13px;color:#6f6a60\">Dërgesa</td><td style=\"padding:10px 0;font-size:13px;color:#6f6a60;text-align:right\">Falas</td></tr>" +
               $"<tr><td style=\"padding:10px 0;font-size:16px\"><strong>Totali</strong></td><td style=\"padding:10px 0;font-size:16px;text-align:right\"><strong>{Money(order.Total)}</strong></td></tr></table>";
    }

    private static string Open(string title) =>
        "<!doctype html><html><body style=\"margin:0;padding:0;background:#f7f4ee;font-family:Arial,Helvetica,sans-serif;color:#0c0c0d\">" +
        "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\"><tr><td align=\"center\" style=\"padding:28px 12px\">" +
        "<table role=\"presentation\" width=\"560\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:560px;width:100%;background:#ffffff;border-radius:18px;overflow:hidden\">" +
        "<tr><td style=\"background:#0c0c0d;padding:22px 28px;text-align:center\"><span style=\"font-family:Georgia,serif;font-size:24px;letter-spacing:7px;color:#f7f4ee\">AMÉ</span><br><span style=\"font-size:10px;letter-spacing:5px;color:#d9c08a\">WATCHES</span></td></tr>" +
        $"<tr><td style=\"padding:30px 28px\"><h1 style=\"margin:0 0 16px;font-family:Georgia,serif;font-size:26px;font-weight:normal\">{title}</h1>";

    private static string Close() => "</td></tr></table></td></tr></table></body></html>";

    private static string PaymentText(Order order) => order.PaymentMethod switch
    {
        PaymentMethod.Card => order.PaymentStatus == PaymentStatus.Paid ? "Me kartë (e paguar)" : "Me kartë",
        _ => "Pagesë në dorëzim"
    };

    private static string Money(decimal value) => value.ToString("0.##", CultureInfo.InvariantCulture) + " €";

    private static string Enc(string? value) => (value ?? string.Empty)
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");

    public static string Clean(string value) => ControlChars().Replace(value, " ").Trim();

    [GeneratedRegex(@"[\p{Cc}\p{Cf}]+")]
    private static partial Regex ControlChars();
}
