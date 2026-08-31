using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace PublicWeb.Seo;

public static class SeoPageApplicator
{
    public static void Apply(SeoPageModel page, ViewDataDictionary viewData, dynamic viewBag)
    {
        viewData["Title"] = page.Title;
        viewData["Description"] = page.Description;
        viewData["CanonicalPath"] = page.CanonicalPath;
        viewData["OgImage"] = page.OgImage;
        viewData["OgTitle"] = page.OgTitle;
        viewData["OgUrl"] = page.OgUrlPath;
        viewData["OgType"] = page.OgType;
        viewData["NoIndex"] = page.NoIndex;
        viewData["LargeOgImage"] = page.LargeOgImage;
        if (!string.IsNullOrWhiteSpace(page.H1))
            viewBag.SeoH1 = page.H1;
        viewBag.SeoBreadcrumbs = page.Breadcrumbs;
    }
}
