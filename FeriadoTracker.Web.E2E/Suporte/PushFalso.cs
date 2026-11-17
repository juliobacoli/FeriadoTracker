using System.Text.Json;
using Microsoft.Playwright;

namespace FeriadoTracker.Web.E2E.Suporte;

/// <summary>
/// Push falso no navegador: troca Notification e navigator.serviceWorker antes de qualquer script da página.
/// O estado (inscrito, permissão, quantas vezes a inscrição foi desfeita) fica no sessionStorage
/// para sobreviver aos reloads do mesmo teste.
/// </summary>
public static class PushFalso
{
    public const string Endpoint = "https://push.e2e.test/sub/1";

    /// <param name="inscrito">Se o aparelho já começa inscrito.</param>
    /// <param name="permissao">Notification.permission inicial: default, granted ou denied.</param>
    /// <param name="respostaPermissao">O que requestPermission devolve.</param>
    /// <param name="locaisSincronizados">Valor inicial de localStorage.locaisPush (ex.: "[]"); null não grava.</param>
    public static Task InstalarAsync(IPage page, bool inscrito, string permissao = "granted",
        string respostaPermissao = "granted", string? locaisSincronizados = "[]")
    {
        var cfg = JsonSerializer.Serialize(new { inscrito, permissao, respostaPermissao, locaisSincronizados, endpoint = Endpoint });
        return page.AddInitScriptAsync($"({Script})({cfg});");
    }

    /// <summary>Quantas vezes a página chamou subscription.unsubscribe().</summary>
    public static Task<int> DesinscricoesAsync(IPage page) =>
        page.EvaluateAsync<int>("() => JSON.parse(sessionStorage.getItem('__pushFalso')).desinscricoes");

    private const string Script = """
        cfg => {
            const CHAVE = '__pushFalso';
            let st = null;
            try { st = JSON.parse(sessionStorage.getItem(CHAVE)); } catch { }
            if (!st) {
                st = { inscrito: cfg.inscrito, permissao: cfg.permissao, desinscricoes: 0 };
                if (cfg.locaisSincronizados !== null) localStorage.setItem('locaisPush', cfg.locaisSincronizados);
            }
            const salvar = () => sessionStorage.setItem(CHAVE, JSON.stringify(st));
            salvar();

            const inscricao = () => ({
                endpoint: cfg.endpoint,
                toJSON: () => ({ endpoint: cfg.endpoint, keys: { p256dh: 'p256dh-e2e', auth: 'auth-e2e' } }),
                unsubscribe: async () => { st.inscrito = false; st.desinscricoes++; salvar(); return true; }
            });
            const registro = {
                pushManager: {
                    getSubscription: async () => st.inscrito ? inscricao() : null,
                    subscribe: async () => { st.inscrito = true; salvar(); return inscricao(); }
                }
            };

            Object.defineProperty(navigator, 'serviceWorker', {
                configurable: true,
                value: {
                    getRegistration: async () => registro,
                    register: async () => registro,
                    ready: Promise.resolve(registro)
                }
            });
            if (!('PushManager' in window)) window.PushManager = function () { };
            window.Notification = class {
                static get permission() { return st.permissao; }
                static async requestPermission() { st.permissao = cfg.respostaPermissao; salvar(); return st.permissao; }
            };
        }
        """;
}
