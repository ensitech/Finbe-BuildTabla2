using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Security.Permissions;
using buildTabla2;

public class ImpersonateLib
{
    //Definicion de variables
    private static ImpersonateLib _instance;
    private IntPtr pTokenAcceso;
    private WindowsImpersonationContext contexto;
    #region "Importando funciones del API de Windows"
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool LogonUser(string username, string dominio, string passwd, Int32 logonType, Int32 logonProvider, ref IntPtr accessToken);

    [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
    internal static extern int CloseHandle(IntPtr hObject);

    #endregion

    private ImpersonateLib()
    {
        contexto = null;
    }

    static ImpersonateLib()
    {
        _instance = new ImpersonateLib();
    }


    public static ImpersonateLib Instance
    {
        get { return _instance; }
        set { _instance = value; }
    }

    public bool IniciarImpersonalizacion(string username, string passwd, string dominio)
    {
        //Definición de variables
        bool status = false;

        //Constantes y valores para el manejo de la impersonalizacion
        const Int32 LOGON32_PROVIDER_DEFAULT = 0;
        const Int32 LOGON32_LOGON_INTERACTIVE = 2;
        pTokenAcceso = new IntPtr(0);
        IntPtr pDuplicateTokeHandle = new IntPtr(0);

        pTokenAcceso = IntPtr.Zero;

        try
        {
            //Iniciando la impersonalizacion
            PConsole.writeLine("username: '" + username + "'");
            //username = "";
            if (username != null && !object.ReferenceEquals(username, string.Empty))
            {
                status = LogonUser(username, dominio, passwd, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref pTokenAcceso);
                PConsole.writeLine("LogonUser");
            }
            else
            {
                pTokenAcceso = WindowsIdentity.GetCurrent(TokenAccessLevels.AllAccess).Token;
                PConsole.writeLine("Current");
            }

            //PConsole.writeLine("is system: "+WindowsIdentity.GetCurrent().IsSystem);

            PConsole.writeLine("pTokenAcceso: " + pTokenAcceso);
            WindowsIdentity identidad = new WindowsIdentity(pTokenAcceso);


            PConsole.writeLine("IsAnonymous: " + identidad.IsAnonymous);
            PConsole.writeLine("IsAuthenticated: " + identidad.IsAuthenticated);
            PConsole.writeLine("IsGuest: " + identidad.IsGuest);
            PConsole.writeLine("IsSystem: " + identidad.IsSystem);
            PConsole.writeLine("IsAccountSid: " + (identidad.User.IsAccountSid()));


            PConsole.writeLine("IsAccountSid: " + identidad.User.Value);
            PConsole.writeLine("actualUser: " + identidad.Name);
            string prevUser = WindowsIdentity.GetCurrent().Name;
            PConsole.writeLine("prevUser: " + prevUser);
            try
            {
                PConsole.writeLine("AuthenticationType: " + identidad.AuthenticationType);
            }
            catch (Exception exAuthenticationType)
            {
                PConsole.writeLine("No se puede leer AuthenticationType: " + exAuthenticationType.Message);
            }
            contexto = null;
            contexto = identidad.Impersonate();

            //IdentityReferenceCollection groups =  identidad.Groups;
            //for (int i = 0; i < groups.Count; i++ ) {
            //    PConsole.writeLine("Group "+(i+1)+": " +groups[i].Value);
            //}
            //SecurityIdentifier owner = identidad.Owner;
            SecurityIdentifier user = identidad.User;
            PConsole.writeLine("identidad.User.Value: " + user.Value);
            //PConsole.writeLine("identidad.Owner.Value: " + owner.Value);
            
        }
        catch (Exception ex)
        {
            throw ex;
        }

        return status;
    }

    public void FinalizarImpersonalizacion()
    {
        try
        {
            if (this.contexto != null)
            {
                contexto.Undo();
                CloseHandle(pTokenAcceso);
            }
        }catch(Exception ex){
            PConsole.writeLine("FinalizarImpersonalizacion: " + ex.Message);
        }
    }

}
