<#--
  Keycloak 26.6.3 base/login/login.ftl üzerine MESNET override'ı.

  DEĞİŞEN TEK ŞEY: "Geçersiz kullanıcı adı veya şifre." mesajının konumu.

  Keycloak varsayılanı bu mesajı ALAN SEVİYESİ hata yuvasına koyuyordu — kullanıcı adı
  input'unun hemen altına (base şablon satır 21-25). Üç sorun:

  1) Anlam yanlış. Mesaj bilinçli olarak belirsizdir (hangisinin hatalı olduğunu söylemek
     kullanıcı adı sayımına kapı açar); kullanıcı adının altına konunca "kullanıcı adın
     yanlış" gibi okunuyor. Oysa hata forma aittir, tek bir alana değil.
  2) Görsel taşma. Yuvanın sınıfı kcInputErrorMessageClass = "... kc-feedback-text" ve
     temamız kc-feedback-text'e kutu stili (padding + kenarlık) veriyor. Element inline
     <span> olduğu için dikey padding satır kutusundan taşıyor, input'un üstüne biniyor.
  3) Erişilebilirlik. Yuvada aria-live="polite" var ama role="alert" yok ve hata hangi
     alanlara ait olduğu input'lara bağlanmamış.

  Çözüm: form seviyesi uyarı olarak formun ÜSTÜNE, role="alert" ile alınır. Her iki input
  aria-invalid ve aria-describedby ile uyarıya bağlanır.

  displayMessage ifadesi base ile AYNI bırakıldı: kimlik hatası varsa şablonun kendi genel
  mesaj bloğu kapanır (bizimki devreye girer), diğer durumlarda (hesap kilitli, bilgi
  mesajı vb.) açık kalır. Böylece mesaj asla iki kez basılmaz.

  BAKIM UYARISI: bu dosya base şablonun kopyasıdır. Keycloak sürümü yükseltilirken
  base/login/login.ftl ile karşılaştırılmalı — yeni alanlar (passkey akışları vb.)
  eklenirse buraya elle taşınmalı.

  Doğrulanan sürümler: 26.6.3, 26.7.0 (base/login/login.ftl ikisinde de birebir aynı;
  jar içindeki dosyalar diff'lenerek kontrol edildi). Bağlı olduğumuz iki nokta —
  template.ftl'deki genel mesaj bloğu ve keycloak/login/theme.properties'teki
  kcAlertClass / kcAlertTitleClass / kcFeedbackErrorIcon / kcInputErrorMessageClass —
  26.7.0'da da değişmedi.
-->
<#import "template.ftl" as layout>
<#import "passkeys.ftl" as passkeys>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
    <#if section = "header">
        ${msg("loginAccountTitle")}
    <#elseif section = "form">
        <div id="kc-form">
          <div id="kc-form-wrapper">

            <#-- Form seviyesi kimlik hatası — alanların ÜSTÜNDE, tek yerde. -->
            <#if messagesPerField.existsError('username','password')>
                <div id="kc-credential-error"
                     class="alert-error ${properties.kcAlertClass!} pf-m-danger kc-credential-error"
                     role="alert">
                    <div class="pf-c-alert__icon">
                        <span class="${properties.kcFeedbackErrorIcon!}" aria-hidden="true"></span>
                    </div>
                    <span class="${properties.kcAlertTitleClass!}">
                        ${kcSanitize(messagesPerField.getFirstError('username','password'))?no_esc}
                    </span>
                </div>
            </#if>

            <#if realm.password>
                <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
                    <#if !usernameHidden??>
                        <div class="${properties.kcFormGroupClass!}">
                            <label for="username" class="${properties.kcLabelClass!}"><#if !realm.loginWithEmailAllowed>${msg("username")}<#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}<#else>${msg("email")}</#if></label>

                            <input tabindex="2" id="username" class="${properties.kcInputClass!}" name="username" value="${(login.username!'')}"  type="text"
                                   autofocus autocomplete="${(enableWebAuthnConditionalUI?has_content)?then('username webauthn', 'username')}"
                                   aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                                   <#if messagesPerField.existsError('username','password')>aria-describedby="kc-credential-error"</#if>
                                   dir="ltr"
                            />
                        </div>
                    </#if>

                    <div class="${properties.kcFormGroupClass!}">
                        <label for="password" class="${properties.kcLabelClass!}">${msg("password")}</label>

                        <div class="${properties.kcInputGroup!}" dir="ltr">
                            <input tabindex="3" id="password" class="${properties.kcInputClass!}" name="password" type="password" autocomplete="current-password"
                                   aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                                   <#if messagesPerField.existsError('username','password')>aria-describedby="kc-credential-error"</#if>
                            />
                            <button class="${properties.kcFormPasswordVisibilityButtonClass!}" type="button" aria-label="${msg("showPassword")}"
                                    aria-controls="password" data-password-toggle tabindex="4"
                                    data-icon-show="${properties.kcFormPasswordVisibilityIconShow!}" data-icon-hide="${properties.kcFormPasswordVisibilityIconHide!}"
                                    data-label-show="${msg('showPassword')}" data-label-hide="${msg('hidePassword')}">
                                <i class="${properties.kcFormPasswordVisibilityIconShow!}" aria-hidden="true"></i>
                            </button>
                        </div>
                    </div>

                    <div class="${properties.kcFormGroupClass!} ${properties.kcFormSettingClass!}">
                        <div id="kc-form-options">
                            <#if realm.rememberMe && !usernameHidden??>
                                <div class="checkbox">
                                    <label>
                                        <#if login.rememberMe??>
                                            <input tabindex="5" id="rememberMe" name="rememberMe" type="checkbox" checked> ${msg("rememberMe")}
                                        <#else>
                                            <input tabindex="5" id="rememberMe" name="rememberMe" type="checkbox"> ${msg("rememberMe")}
                                        </#if>
                                    </label>
                                </div>
                            </#if>
                            </div>
                            <div class="${properties.kcFormOptionsWrapperClass!}">
                                <#if realm.resetPasswordAllowed>
                                    <span><a tabindex="6" href="${url.loginResetCredentialsUrl}">${msg("doForgotPassword")}</a></span>
                                </#if>
                            </div>

                      </div>

                      <div id="kc-form-buttons" class="${properties.kcFormGroupClass!}">
                          <input type="hidden" id="id-hidden-input" name="credentialId" <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if>/>
                          <input tabindex="7" class="${properties.kcButtonClass!} ${properties.kcButtonPrimaryClass!} ${properties.kcButtonBlockClass!} ${properties.kcButtonLargeClass!}" name="login" id="kc-login" type="submit" value="${msg("doLogIn")}"/>
                      </div>
                </form>
            </#if>
            </div>
        </div>
        <@passkeys.conditionalUIData />
        <script type="module" src="${url.resourcesPath}/js/passwordVisibility.js"></script>
    <#elseif section = "info" >
        <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
            <div id="kc-registration-container">
                <div id="kc-registration">
                    <span>${msg("noAccount")} <a tabindex="8"
                                                 href="${url.registrationUrl}">${msg("doRegister")}</a></span>
                </div>
            </div>
        </#if>
    <#elseif section = "socialProviders" >
        <#if realm.password && social?? && social.providers?has_content>
            <div id="kc-social-providers" class="${properties.kcFormSocialAccountSectionClass!}">
                <hr/>
                <h2>${msg("identity-provider-login-label")}</h2>

                <ul class="${properties.kcFormSocialAccountListClass!} <#if social.providers?size gt 3>${properties.kcFormSocialAccountListGridClass!}</#if>">
                    <#list social.providers as p>
                        <li>
                            <a data-once-link data-disabled-class="${properties.kcFormSocialAccountListButtonDisabledClass!}" id="social-${p.alias}"
                                    class="${properties.kcFormSocialAccountListButtonClass!} <#if social.providers?size gt 3>${properties.kcFormSocialAccountGridItem!}</#if>"
                                    type="button" href="${p.loginUrl}">
                                <#if p.iconClasses?has_content>
                                    <i class="${properties.kcCommonLogoIdP!} ${p.iconClasses!}" aria-hidden="true"></i>
                                    <span class="${properties.kcFormSocialAccountNameClass!} kc-social-icon-text">${p.displayName!}</span>
                                <#else>
                                    <span class="${properties.kcFormSocialAccountNameClass!}">${p.displayName!}</span>
                                </#if>
                            </a>
                        </li>
                    </#list>
                </ul>
            </div>
        </#if>
    </#if>

</@layout.registrationLayout>
