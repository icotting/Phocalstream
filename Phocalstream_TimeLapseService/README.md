##The PhocalStream Timelapse Generation Service##
This is a Windows Service that can be used to generate timelapses from a set of photo ids. WCF is used to communicate with the time lapse generation service.

###WCF Communication###
A basic example of usage is in the ```Test``` class:

```C#
static void Main(string[] args)
{
    ITimeLapseManager manager = (ITimeLapseManager)Activator.GetObject(typeof(ITimeLapseManager), "tcp://localhost:8084/TimeLapseManager");
    List<long> ids = new List<long>();
    for(int i = 45; i <= 77; ++i)
    {
        ids.Add(i);
    }
    long job = manager.StartJob(ids, 20);
    Console.WriteLine(manager.GetJobDestination(job));
}
```

This basic example connects to the ```TimeLapseManager```, requests that a ```TimeLapseJob``` be started, and then prints out the location where you will find the completed MPEG timelapse.

###Configuration####
In App.config there are several important path specifiers.

magickPath: This is the path to your ImageMagick installation, e.g. "[...]/ImageMagick-6.88-Q16"

ffmpegPath: Similarly this is the path to your ffmpeg installation, e.g. "[...]/ffmpeg/bin"

outputPath: This is the path to where you want the service's output to go. Output is described in the output section.

photoPath: This is the path to the photo database with which to make timelapses, e.g. "[...]/Phocalstream/Phocalstream_Web/App_Data/Photo_Data/Phocalstream/"

rawPath: This is the path to the photo raws with which to make timelapses, e.g. "[...]/Phocalstream/Phocalstream_Web/App_Data/Photo_Data/Timelapse/"

####Condor####
It is difficult to say exactly how the project must be configured for Condor, as different Condor pools have different settings. The place where one can change the way condor submit files are generated is in the ```CreateBlend``` function in ```TimeLapseJob```.

As an aside, I will note that it will likely be extremely difficult to set up a Condor test server on a Windows machine, due to minimal documentation and a multitude of compatability issues. I highly recommend avoiding this problem by using a unix base operating system when creating your Condor test server.

###Ongoing work###
In the ```TimeLapseJob```, ```ExtractImagePath``` is a hack which solved a problem that is now fixed. This needs to be fixed so that the new relative directories work along with the ```TimeLapseJob```.

```CheckJob``` in the ```TimeLapseManager``` isn't functional now that condor is used. It needs to be altered so that instead of depending on C# processes adding to a ```TimeLapseJob```'s completion, ```CheckJob``` polls the number of files in the temporary file directory and answers using that.

The way that the ```TimeLapseManager``` takes in input is very limited. If the service is down, any incoming requests would be lost. Ways of improving this could be changing the way of specifying jobs to be a table in the database.

There is a great deal of string concatenation in ```TimeLapseJob```. This should probably be fixed, depending on how the .NET runtime deals with strings.

This is low priority, but the ```ProcessPhotos``` function in ```TimeLapseJob``` could be improved. At the moment, it polls the number of files in the temporary directory to see whether it has finished blending images. It could instead sign up with the operating system so that it doesn't poll more than necessary. Larger jobs could also have the polling rate lowered because sampling error would be less impactful.