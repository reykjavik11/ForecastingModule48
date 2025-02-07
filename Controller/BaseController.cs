namespace ForecastingModule.Controller
{
    internal abstract class BaseController
    {
        protected object model{get; set;}
        protected IView view {get; set;}

        public abstract object Load();
    
        public BaseController(object model, IView view)
        {
            this.model = model;
            this.view = view;
        }
    }
}