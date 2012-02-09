﻿using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Griffin.MvcContrib.Areas.Griffin.Models.LocalizeTypes;
using Griffin.MvcContrib.Localization;
using Griffin.MvcContrib.Localization.Types;
using EditModel = Griffin.MvcContrib.Areas.Griffin.Models.LocalizeTypes.EditModel;
using IndexModel = Griffin.MvcContrib.Areas.Griffin.Models.LocalizeTypes.IndexModel;

namespace Griffin.MvcContrib.Areas.Griffin.Controllers
{
    [Localized]
    public class LocalizeTypesController : System.Web.Mvc.Controller
    {
        private readonly ILocalizedTypesRepository _repository;

        public LocalizeTypesController(ILocalizedTypesRepository repository)
        {
            _repository = repository;
        }

        [HttpPost]
        public ActionResult CreateLanguage(string lang)
        {
            try
            {

                _repository.CreateLanguage(new CultureInfo(lang), DefaultCulture.Value);
                return RedirectToAction("Index", new { lang = lang });
            }
            catch (Exception err)
            {
                ModelState.AddModelError("", err.Message);
                var allPrompts = _repository.GetPrompts(CultureInfo.CurrentUICulture, DefaultCulture.Value, new SearchFilter());
                var model = new IndexModel
                {
                    Cultures = _repository.GetAvailableLanguages(),
                    Prompts = allPrompts.Select(p => new TypePrompt(p))
                };
                return View("Index", model);
            }
        }

        public ActionResult Index()
        {
            var cookie = Request.Cookies["ShowMetadata"];
            var showMetadata = cookie != null && cookie.Value == "1";

            var languges = _repository.GetAvailableLanguages();

            var prompts =
                _repository.GetPrompts(CultureInfo.CurrentUICulture, DefaultCulture.Value, new SearchFilter()).Select(
                    p => new TypePrompt(p)).OrderBy(p => p.TypeName).
                    ToList();
            if (!showMetadata)
                prompts = prompts.Where(p => p.TextName == null || !p.TextName.Contains("_")).ToList();

            var model = new IndexModel
                            {
                                Prompts = prompts,
                                Cultures = languges,
                                ShowMetadata = showMetadata
                            };
            return View(model);
        }

        public ActionResult Edit(string id)
        {
            var model = CreateModel(id);
            return View(model);
        }

        private EditModel CreateModel(string id)
        {
            var key = new TypePromptKey(id);
            var prompt = _repository.GetPrompt(CultureInfo.CurrentUICulture, key);
            var defaultLang = _repository.GetPrompt(DefaultCulture.Value, key);
            var model = new EditModel
                            {
                                DefaultText = defaultLang != null ? defaultLang.TranslatedText : "",
                                LocaleId = prompt.LocaleId,
                                Path = string.Format("{0} / {1} / {2}", CultureInfo.CurrentUICulture.DisplayName, prompt.Subject.Name, prompt.TextName),
                                Text = prompt.TranslatedText,
                                TextKey = prompt.Key.ToString()
                            };
            return model;
        }

        [HttpPost]
        public ActionResult Edit(TranslateModel inmodel)
        {
            var model = CreateModel(inmodel.TextKey);
            model.Text = inmodel.Text;
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            if (!ModelState.IsValid)
                return View(model);

            try
            {
                _repository.Update(CultureInfo.CurrentUICulture, new TypePromptKey(model.TextKey), model.Text);
                return RedirectToAction("Index");
            }
            catch (Exception err)
            {
                ModelState.AddModelError("", err.Message);
                return View(model);
            }
        }
    }
}
