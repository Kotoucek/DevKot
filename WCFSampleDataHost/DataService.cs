using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WCFSampleDataService
{    
    public class DataService : ServiceHost,  IDataService, IDataServiceV2
    {
        public static Dictionary<Customer, IDataCallback> verbindungen = new Dictionary<Customer, IDataCallback>();
        public static BindingList<String> bindingStringList = new BindingList<String>();
       
        public DataService()
        {
            bindingStringList.AddingNew += BindingStringList_AddingNew;
            //t3 = new Task(() =>
            //{
            //    AusgabeAlleVerbindungen();

            //});

            //t3.Start();

        }

        private void BindingStringList_AddingNew(object sender, AddingNewEventArgs e)
        {
            Console.WriteLine("Add new Objekt: " + e.NewObject.ToString());
            t4 = new Task(() =>
            {

                foreach (var ver in verbindungen)
                {

                    if (ver.Value != null)
                    {
                        try
                        {
                            ver.Value.OnCustomerBroadCast(e.NewObject.ToString());
                         
                        }
                        catch (CommunicationException ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ver.Key.Name);
                        }
                        catch (TimeoutException extime)
                        {
                            Console.WriteLine(String.Format("Konnte nicht an: {0} versendet werden", ver.Key.Name));
                        }
                    }
                }
            });
            t4.Start();
        }

        private Customer GetCustomer(int customerId)
        {
            // delay to simulate a downstream web service call
            Thread.CurrentThread.Join(500);

            return new Customer
            {
                CustomerId = customerId,
                Name = string.Format("Customer '{0}'", customerId)
            };
        }
        private Customer GetCustomer(int customerId, String name)
        {
            // delay to simulate a downstream web service call
            Thread.CurrentThread.Join(500);

            return new Customer
            {
                CustomerId = customerId,
                Name = name
            };
        }
        private CustomerAddress GetCustomerAddress(Customer customer)
        {
            // delay to simulate a downstream web service call
            Thread.CurrentThread.Join(300);

            return new CustomerAddress
            {
                CustomerId = customer.CustomerId,
                Address = string.Format("Customer '{0}' Address", customer.CustomerId)
            };
        }

        private CustomerInvoices GetCustomerInvoices(Customer customer)
        {
            // delay to simulate a downstream web service call
            // this is a slow legacy system hobbled together and held firm with duct tape
            Thread.CurrentThread.Join(800);

            return new CustomerInvoices
            {
                CustomerId = customer.CustomerId,
                Invoices = new CustomerInvoice[]
                    {
                        new CustomerInvoice
                            {
                                InvoiceId = 1,
                                InvoiceDate = DateTime.Now.Date.AddMonths(-2),
                                Amount = 100
                            },
                            new CustomerInvoice
                            {
                                InvoiceId = 2,
                                InvoiceDate = DateTime.Now.Date.AddMonths(-1),
                                Amount = 200
                            },
                    }
            };
        }

        String IDataServiceV2.BroadCastToAllCustomers(string message)
        {
            IDataCallback callback = OperationContext.Current.GetCallbackChannel<IDataCallback>();
            String confiormation = "";
            t4 = new Task(()=>
            {
            
                foreach(var ver in verbindungen)
                {
                   
                        if (ver.Value != null)
                        {
                        try
                        {
                            ver.Value.OnCustomerBroadCast(message);
                            confiormation += String.Format(": {0}", ver.Key.Name);
                        }
                        catch(CommunicationException ex)
                        {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ver.Key.Name);
                        }
                        catch(TimeoutException extime)
                        {
                            Console.WriteLine(String.Format("Konnte nicht an: {0} versendet werden", ver.Key.Name));
                        }
                        }
                }
            });
            t4.Start();
            return confiormation;
        }

        Customer IDataServiceV2.GetCustomer(int customerId,String name)
        {
            bindingStringList.AllowNew = true;
            bindingStringList.AddingNew += null;
          //  bindingStringList.AddingNew += BindingStringList_AddingNew1;


            IDataCallback callback = OperationContext.Current.GetCallbackChannel<IDataCallback>();

            bindingStringList.ListChanged += null;
            bindingStringList.ListChanged += BindingStringList_ListChanged;

            bindingStringList.Add("Add " + name);
            Customer customer = GetCustomer(customerId,name);
            if (verbindungen.Count != 0)
            {
                Console.WriteLine("Temp: "+verbindungen.Count.ToString());
                foreach (var ver in verbindungen)
                {

                    if (ver.Key.Name == name)
                    {
                        verbindungen.Remove(ver.Key);
                        Console.WriteLine("Aus Liste gelöscht: {0}", customer.Name);
                        break;
                    }
                }
            }
            verbindungen.Add(customer, callback);
            t1 = new Task(() =>
            {
                var address = GetCustomerAddress(customer);

                if (callback == null)
                    Console.WriteLine("DataService.GetCustomer({0}) failed to get Address callback channel!", customerId);
                else
                {
                    callback.OnCustomerAddressComplete(address);
                  
                }
            });

            t1.Start();

            t2 = new Task(() =>
            {
                var invoices = GetCustomerInvoices(customer);

                if (callback == null)
                    Console.WriteLine("DataService.GetCustomer({0}) failed to get Invoices callback channel!", customerId);
                else
                    callback.OnCustomerInvoicesComplete(invoices);
               
            });
            t2.Start();
            t3 = new Task(() =>
                {

                    Console.WriteLine("Anzahl Verbindungen: {0}", verbindungen.Count.ToString());
                    AusgabeAlleVerbindungen();

                });
            t3.Start();
            return customer;
        }

        public void BindingStringList_ListChanged(object sender, ListChangedEventArgs e)
        {
            Console.WriteLine("List new Objekt: " + e.NewIndex.ToString());
        }

        public void BindingStringList_AddingNew1(object sender, AddingNewEventArgs e)
        {
            Console.WriteLine("Add new Objekt: " + e.NewObject.ToString());

        }

        void IDataServiceV2.VerbindungTrennen(int customerId)
        {
           

            lock (verbindungen)
            {
                foreach (var ver in verbindungen)
                {

                    if (ver.Key.CustomerId == customerId)
                    {
                        verbindungen.Remove(ver.Key);
                        Console.WriteLine("Aus Liste gelöscht: {0}", ver.Key.Name.ToString());
                        break;
                       
                      

                    }

                }
            }



        }
        private void AusgabeAlleVerbindungen()
        {
            String nocallback;
            foreach(var verbindung in verbindungen)
            {
                
                if(verbindung.Value==null)
                {

                    nocallback = "Unterbrochen";
                }
                else
                {
                    nocallback = "aufrecht";
                }

                Console.WriteLine("Name: {0} mit der Id {1} ist {2}", verbindung.Key.Name, verbindung.Key.CustomerId.ToString(),nocallback);

            }

        }

        Customer IDataService.GetCustomer(int customerId)
        {
            return GetCustomer(customerId);
        }

        CustomerAddress IDataService.GetCustomerAddress(int customerId)
        {
            Customer customer = GetCustomer(customerId);
            return GetCustomerAddress(customer);
        }

        CustomerInvoices IDataService.GetCustomerInvoices(int customerId)
        {
            Customer customer = GetCustomer(customerId);
            return GetCustomerInvoices(customer);
        }

        // TODO: investigate thread management but based on msdn, this should be ok for a sample: http://blogs.msdn.com/b/pfxteam/archive/2012/03/25/10287435.aspx
        private Task t1;
        private Task t2;
        private Task t3;
        private Task t4;
        protected override void OnClosed()
        {
            if(t1!=null)
                //t1.Dispose();

            if(t2!=null)
                t2.Dispose();
            if (t3 != null)
                t3.Dispose();

            base.OnClosed();
        }
        
    }
}
