using Markdig.Renderers;
using Markdig.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Neko.Extensions
{
    /// <summary>
    /// Renders a ```quiz fenced block: a self-scoring multiple-choice
    /// comprehension check. Single-answer questions use radio inputs;
    /// questions with multiple correct answers use checkboxes. Scoring,
    /// explanation reveal, and "answered" state are handled client-side and
    /// persisted per-browser in localStorage. There is no backend.
    /// </summary>
    public static class QuizComponent
    {
        private class QuizModel
        {
            [YamlMember(Alias = "id")] public string Id { get; set; }
            [YamlMember(Alias = "title")] public string Title { get; set; }
            [YamlMember(Alias = "questions")] public List<QuizQuestion> Questions { get; set; } = new();
        }

        private class QuizQuestion
        {
            [YamlMember(Alias = "q")] public string Q { get; set; }
            [YamlMember(Alias = "options")] public List<string> Options { get; set; } = new();
            // Single correct answer (0-based index).
            [YamlMember(Alias = "answer")] public int? Answer { get; set; }
            // Multiple correct answers (0-based indices).
            [YamlMember(Alias = "answers")] public List<int> Answers { get; set; }
            [YamlMember(Alias = "explain")] public string Explain { get; set; }
        }

        public static void Write(HtmlRenderer renderer, FencedCodeBlock fencedBlock)
        {
            var yaml = ReadBody(fencedBlock);

            QuizModel model;
            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                model = deserializer.Deserialize<QuizModel>(yaml) ?? new QuizModel();
            }
            catch (System.Exception ex)
            {
                renderer.Write("<div class=\"not-prose my-6 rounded-xl border border-rose-300 bg-rose-50 dark:bg-rose-900/20 p-4 text-sm text-rose-700 dark:text-rose-300\">Quiz could not be parsed: ");
                renderer.Write(WebUtility.HtmlEncode(ex.Message));
                renderer.Write("</div>");
                return;
            }

            if (model.Questions == null || model.Questions.Count == 0)
            {
                renderer.Write("<div class=\"not-prose my-6 rounded-xl border border-amber-300 bg-amber-50 dark:bg-amber-900/20 p-4 text-sm text-amber-700 dark:text-amber-300\">Quiz has no questions.</div>");
                return;
            }

            var quizId = string.IsNullOrEmpty(model.Id) ? DeriveId(model) : model.Id;
            var title = string.IsNullOrEmpty(model.Title) ? "Check yourself" : model.Title;
            var total = model.Questions.Count;

            renderer.Write("<div class=\"not-prose my-8 rounded-2xl border border-gray-200 dark:border-gray-700 p-6\" data-neko-quiz=\"");
            renderer.Write(WebUtility.HtmlEncode(quizId));
            renderer.Write("\" data-quiz-total=\"");
            renderer.Write(total.ToString());
            renderer.Write("\">");

            // Header
            renderer.Write("<div class=\"flex items-center gap-3 mb-5\">");
            renderer.Write("<div class=\"flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary-500/15 ring-1 ring-primary-500/30 text-primary-500\"><i class=\"fi fi-rr-graduation-cap text-base leading-none\"></i></div>");
            renderer.Write("<h3 class=\"text-lg font-bold tracking-tight text-gray-900 dark:text-white m-0\">");
            renderer.Write(WebUtility.HtmlEncode(title));
            renderer.Write("</h3></div>");

            renderer.Write("<ol class=\"space-y-6 list-none m-0 p-0\">");

            for (int i = 0; i < model.Questions.Count; i++)
            {
                var question = model.Questions[i];
                var correct = CorrectIndices(question);
                var multi = correct.Count > 1;
                var inputType = multi ? "checkbox" : "radio";

                renderer.Write("<li class=\"rounded-xl border border-gray-100 dark:border-gray-800 p-4\" data-quiz-q=\"");
                renderer.Write(i.ToString());
                renderer.Write("\" data-quiz-correct=\"");
                renderer.Write(string.Join(",", correct));
                renderer.Write("\">");

                renderer.Write("<div class=\"flex items-start gap-3 mb-3\">");
                renderer.Write($"<span class=\"font-mono text-xs text-gray-500 dark:text-gray-400 mt-1\">{(i + 1).ToString("D2")}</span>");
                renderer.Write("<div class=\"font-medium text-gray-900 dark:text-gray-100\">");
                renderer.Write(WebUtility.HtmlEncode(question.Q));
                if (multi) renderer.Write(" <span class=\"text-xs font-normal text-gray-400\">(select all that apply)</span>");
                renderer.Write("</div></div>");

                renderer.Write("<div class=\"space-y-2 pl-7\">");
                for (int j = 0; j < question.Options.Count; j++)
                {
                    var optionName = $"q-{quizId}-{i}";
                    renderer.Write("<label class=\"flex items-start gap-3 rounded-lg px-3 py-2 cursor-pointer border border-transparent hover:bg-gray-50 dark:hover:bg-gray-800/40 transition-colors\" data-quiz-option=\"");
                    renderer.Write(j.ToString());
                    renderer.Write("\">");
                    renderer.Write($"<input type=\"{inputType}\" name=\"{WebUtility.HtmlEncode(optionName)}\" value=\"{j}\" class=\"mt-1 shrink-0\" data-quiz-input>");
                    renderer.Write("<span class=\"text-sm text-gray-700 dark:text-gray-300\">");
                    renderer.Write(WebUtility.HtmlEncode(question.Options[j]));
                    renderer.Write("</span>");
                    renderer.Write("<i class=\"fi fi-rr-check ml-auto text-emerald-500 hidden\" data-quiz-mark-correct></i>");
                    renderer.Write("<i class=\"fi fi-rr-cross-small ml-auto text-rose-500 hidden\" data-quiz-mark-wrong></i>");
                    renderer.Write("</label>");
                }
                renderer.Write("</div>");

                if (!string.IsNullOrEmpty(question.Explain))
                {
                    renderer.Write("<div class=\"hidden mt-3 ml-7 rounded-lg bg-gray-50 dark:bg-gray-800/60 px-3 py-2 text-sm text-gray-600 dark:text-gray-300\" data-quiz-explain>");
                    renderer.Write(WebUtility.HtmlEncode(question.Explain));
                    renderer.Write("</div>");
                }

                renderer.Write("</li>");
            }

            renderer.Write("</ol>");

            // Controls
            renderer.Write("<div class=\"flex items-center gap-3 mt-6\">");
            renderer.Write("<button type=\"button\" data-quiz-check class=\"inline-flex items-center rounded-md bg-primary-600 hover:bg-primary-500 px-4 py-2 text-sm font-semibold text-white transition-colors\">Check answers</button>");
            renderer.Write("<button type=\"button\" data-quiz-reset class=\"inline-flex items-center rounded-md px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-200 ring-1 ring-gray-200 dark:ring-gray-700 hover:ring-gray-300 dark:hover:ring-gray-500 transition-colors\">Reset</button>");
            renderer.Write("<span data-quiz-score class=\"text-sm font-medium text-gray-600 dark:text-gray-300\"></span>");
            renderer.Write("</div>");

            renderer.Write("<script>");
            renderer.Write(GetClientScript());
            renderer.Write("</script>");

            renderer.Write("</div>");
        }

        private static List<int> CorrectIndices(QuizQuestion question)
        {
            if (question.Answers != null && question.Answers.Count > 0)
                return question.Answers.Distinct().OrderBy(x => x).ToList();
            if (question.Answer.HasValue)
                return new List<int> { question.Answer.Value };
            return new List<int>();
        }

        private static string ReadBody(FencedCodeBlock fencedBlock)
        {
            var sb = new StringBuilder();
            var lines = fencedBlock.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                var slice = lines.Lines[i].Slice;
                if (slice.Text == null) continue;
                sb.AppendLine(slice.ToString());
            }
            return sb.ToString();
        }

        // Deterministic id from question text so progress survives rebuilds.
        private static string DeriveId(QuizModel model)
        {
            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                uint hash = offset;
                void Mix(string s)
                {
                    if (s == null) return;
                    foreach (var c in s) { hash ^= c; hash *= prime; }
                }
                Mix(model.Title);
                foreach (var q in model.Questions)
                {
                    Mix(q.Q);
                    if (q.Options != null) foreach (var o in q.Options) Mix(o);
                }
                return "quiz-" + hash.ToString("x8");
            }
        }

        public static string GetClientScript()
        {
            return @"(function(){
  if (window.__nekoQuizInit) return; window.__nekoQuizInit = true;
  function key(id){ return 'neko-quiz:' + id; }
  function load(id){ try { return JSON.parse(localStorage.getItem(key(id))) || {}; } catch(e){ return {}; } }
  function save(id, state){ try { localStorage.setItem(key(id), JSON.stringify(state)); } catch(e){} }
  function hydrate(root){
    var id = root.getAttribute('data-neko-quiz');
    var total = parseInt(root.getAttribute('data-quiz-total') || '0', 10);
    var questions = Array.prototype.slice.call(root.querySelectorAll('[data-quiz-q]'));
    var scoreEl = root.querySelector('[data-quiz-score]');
    function grade(persist){
      var correctCount = 0;
      questions.forEach(function(li){
        var correct = (li.getAttribute('data-quiz-correct') || '').split(',').filter(function(x){return x !== '';}).map(Number);
        var selected = [];
        li.querySelectorAll('[data-quiz-input]').forEach(function(inp, idx){ if (inp.checked) selected.push(idx); });
        var ok = correct.length === selected.length && correct.every(function(c){ return selected.indexOf(c) !== -1; });
        if (ok) correctCount++;
        li.querySelectorAll('[data-quiz-option]').forEach(function(opt, idx){
          var isCorrect = correct.indexOf(idx) !== -1;
          var mc = opt.querySelector('[data-quiz-mark-correct]');
          var mw = opt.querySelector('[data-quiz-mark-wrong]');
          if (mc) mc.classList.add('hidden');
          if (mw) mw.classList.add('hidden');
          opt.classList.remove('border-emerald-300','bg-emerald-50','dark:bg-emerald-900/20','border-rose-300','bg-rose-50','dark:bg-rose-900/20');
          var inp = opt.querySelector('[data-quiz-input]');
          if (isCorrect){ opt.classList.add('border-emerald-300','bg-emerald-50','dark:bg-emerald-900/20'); if (mc) mc.classList.remove('hidden'); }
          else if (inp && inp.checked){ opt.classList.add('border-rose-300','bg-rose-50','dark:bg-rose-900/20'); if (mw) mw.classList.remove('hidden'); }
        });
        var ex = li.querySelector('[data-quiz-explain]'); if (ex) ex.classList.remove('hidden');
      });
      if (scoreEl) scoreEl.textContent = correctCount + ' / ' + total + ' correct';
      if (persist) save(id, { answered: true, score: correctCount });
      return correctCount;
    }
    var checkBtn = root.querySelector('[data-quiz-check]');
    if (checkBtn) checkBtn.addEventListener('click', function(){ grade(true); });
    var resetBtn = root.querySelector('[data-quiz-reset]');
    if (resetBtn) resetBtn.addEventListener('click', function(){
      root.querySelectorAll('[data-quiz-input]').forEach(function(inp){ inp.checked = false; });
      root.querySelectorAll('[data-quiz-option]').forEach(function(opt){
        opt.classList.remove('border-emerald-300','bg-emerald-50','dark:bg-emerald-900/20','border-rose-300','bg-rose-50','dark:bg-rose-900/20');
        opt.querySelectorAll('[data-quiz-mark-correct],[data-quiz-mark-wrong]').forEach(function(m){ m.classList.add('hidden'); });
      });
      root.querySelectorAll('[data-quiz-explain]').forEach(function(ex){ ex.classList.add('hidden'); });
      if (scoreEl) scoreEl.textContent = '';
      save(id, {});
    });
    var state = load(id);
    if (state && state.answered && scoreEl){ scoreEl.textContent = 'Previously answered: ' + (state.score || 0) + ' / ' + total; }
  }
  function init(){ document.querySelectorAll('[data-neko-quiz]').forEach(hydrate); }
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
})();";
        }
    }
}
