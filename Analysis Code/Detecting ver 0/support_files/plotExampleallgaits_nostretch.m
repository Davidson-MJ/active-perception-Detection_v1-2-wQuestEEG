% plotExampleAllGaits_nostretch



%%%%%% QUEST DETECT version %%%%%%
%Mac:
% datadir='/Users/matthewdavidson/Documents/GitHub/active-perception-Detection_v1-1wQuest/Analysis Code/Detecting ver 0/Raw_data';
%PC:
datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';
% laptop:
% datadir='C:\Users\vrlab\Documents\GitHub\active-perception-Detection_v1-1wQuest\Analysis Code\Detecting ver 0\Raw_data';


cd([datadir filesep 'ProcessedData'])

pfols = dir([pwd filesep '*summary_data.mat']);
nsubs= length(pfols);
%show ppant numbers:
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)

%% concat data:
%preallocate storage:

pidx1=ceil(linspace(1,100,11)); % length n-1
% pidx2= ceil(linspace(1,200,14));% length n-1 (was 13)
pidx2= ceil(linspace(1,200,21));% 

gaittypes = {'single gait' , 'double gait'};
%


for ippant =1%:nsubs
    cd([datadir filesep 'ProcessedData'])    %%load data from import job.
    load(pfols(ippant).name, ...
        'Ppant_gaitData', 'Ppant_gaitData_doubGC','PFX_headY', 'PFX_headY_doubleGC', 'subjID');
    
    subjIDs{ippant} = subjID;
    
    disp(['showing subject ' subjID]);
    
    figure(1);
    subplot(211)
    hold on;
   
        
%     for igait 
    
    
    
end