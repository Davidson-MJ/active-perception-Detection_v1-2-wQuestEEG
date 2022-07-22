%quick calibration check:

%quickly load raw csv (summary), and plot the results of calibration.

%%
%%%%%% QUEST & EEG version

% Detection experiment (contrast)
%%  Import from csv. FramebyFrame, then summary data.

%frame by frame first:
%PC
datadir='C:\Users\User\Documents\matt\GitHub\active-perception-Detection_v1-2-wQuestEEG\Analysis Code\Detecting ver 0\Raw_data';
  
cd(datadir)
pfols = dir([pwd filesep '*trialsummary.csv']);
nsubs= length(pfols);
tr= table([1:length(pfols)]',{pfols(:).name}' );
disp(tr)

%
%% Per csv file, import and wrangle into Matlab Structures, and data matrices:
for ippant =4%:length(pfols)
  
cd(datadir)
pfols = dir([pwd filesep '*trialsummary.csv']);
nsubs= length(pfols);
%
filename = pfols(ippant).name;
  
    %extract name&date from filename:
    ftmp = find(filename =='_');
    subjID = filename(1:ftmp(end)-1);
    %read table
    opts = detectImportOptions(filename,'NumHeaderLines',0);
    T = readtable(filename,opts);
    rawSummary_table = T;
    disp(['Preparing participant ' T.participant{1} ]);
    %%
    
    savename = [subjID '_summary_data'];
    % summarise relevant data:
    % calibration is performed after every dual target presented
    targPrestrials =find(T.nTarg>0);
    practIndex = find(T.isPrac ==1);
    nPracBlocks = unique(T.block(practIndex));
    nPrac=length(nPracBlocks);
    ss= size(practIndex,1);
    practIndex(ss+1) = practIndex(ss)+1;
    
 
    %%
    disp([subjID ' has ' num2str((T.trial(practIndex(end)) +1)) ' practice trials']);
    %extract the rows in our table, with relevant data for assessing
    %calibration:
   
     %% figure 1
    figure(1);  clf; 
    set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .5 .5]);
    cols= {'r', 'b', 'k'};
    lg=[];
    hold on;
   stairTypes = unique(T.qStaircase);
   %
   pcounter=1;
   for ipracblock = 1:nPrac
       for iqstair=1:length(stairTypes)% check all staircases.
           usestair= stairTypes(iqstair);
           qstairtrials= find(T.qStaircase==usestair);
           blckindx = find(T.block == nPracBlocks(ipracblock));
           calibAxis = intersect(qstairtrials, blckindx);
           
           if isempty(calibAxis)
               continue
           end
           %calculate accuracy:
           calibData = T.targCor(calibAxis);
           calibAcc = zeros(1, length(calibData));
           for itarg=1:length(calibData)
               tmpD = calibData(1:itarg);
               calibAcc(itarg) = sum(tmpD)/length(tmpD);
           end
           
           %retain contrast values:
           calibContrast = T.targContrast(calibAxis);
           % also show the contrast used after staircase:
           exprows = find(T.isPrac==0);
           exprows=exprows(2:end); % remove first target out of staircase.
           
    
           %%
           subplot(nPrac,2,pcounter);
           plot(calibContrast, 'o-', 'color', cols{iqstair});
           title('contrast'); hold on; ylabel('contrast')
           xlabel('target count');
           % add the final contr values:
           %  expContrasts= unique(T.targContrast(exprows));
           % for ic= 1:length(expContrasts)
           % plot(xlim, [expContrasts(ic), expContrasts(ic)], ['r:']);
           % end
           
           subplot(nPrac,2,pcounter+1);
           lg(iqstair) = plot(calibAcc, 'o-', 'color', cols{iqstair}); title('Accuracy');
           hold on; ylim([0 1])
           xlabel('target count');
           title(['Block ' num2str(ipracblock)])

       end
       pcounter=pcounter+2;
   end
   
   %%
   cd([datadir filesep 'Figures' filesep 'Calibration'])
   print('-dpng', [subjID ' quick summary'])
    %%
    
    %%
%     legend(lg, 'autoupdate', 'off');
    
    %% now plot accuracy standing and walking.
    %find standing and walking trials.
    strows= find(T.isStationary==1);
    wkrows= find(T.isStationary==0);
   
    %intersect of experiment, and relevant condition:
    standingrows = intersect(strows, exprows);
    walkingrows = intersect(wkrows, exprows);
    stblockIDs= find(diff(standingrows)>1);
    wkblockIDs= find(diff(wkrows)>1);
    %%
    standingAcc = sum(T.targCor(standingrows))/ length(standingrows);
   
    walkAcc = sum(T.targCor(walkingrows))/ length(walkingrows);
    
    
    %% also plot the result, just for the central staircase:
    
    stairResult = find(T.targContrastPosIdx==3);
    midStair_standrows = intersect(stairResult, standingrows);
     midStair_walkrows = intersect(stairResult, walkingrows);

     midstandAcc =  sum(T.targCor(midStair_standrows))/ length(midStair_standrows);
     midwalkAcc  =  sum(T.targCor(midStair_walkrows))/ length(midStair_walkrows);

   %%
    figure(2);  clf; 
    set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .5 .5]);
   
    subplot(231)
    bar([standingAcc, walkAcc])
    hold on;
    plot(1, midstandAcc, 'r-o')
    plot(2, midwalkAcc, 'r-o')
    
    shg
    %%
    title(subjID);
    set(gca,'xticklabels', {'standing', 'walking'})
    ylabel('meanAccuracy');
    ylim([.2 1]);
    hold on;
        
    text(1-.25, standingAcc*1.05, [num2str(round(standingAcc,2))]);
    text(2-.25, walkAcc*1.05, [num2str(round(walkAcc,2))]);
    
    %% also RT
    allRTs= T.targRT - T.targOnset;
    %remove negatives(these were no resp).
    allRTs(allRTs<0) = nan;
    
     standingRT = nansum(allRTs(standingrows))/ length(standingrows);   
     walkRT = nansum(allRTs(walkingrows))/ length(walkingrows);
    
    subplot(232)
    bar([standingRT, walkRT], 'Facecolor', 'r')
    shg
    title(subjID);
    set(gca,'xticklabels', {'standing', 'walking'})
    ylabel('meanRT');
    
    text(1-.25, standingRT*1.05, [num2str(round(standingRT,2))]);
    text(2-.25, walkRT*1.05, [num2str(round(walkRT,2))]);
    
   ylim([0 .7])
    
%     %% plot PF for standing and walking:
%     % now we can extract the types correct for each.
%     %whole exp first:
%%     exprows=standingrows;
% figure()
%%
%% note that in contrast pos 0,1,2,3,4,5,6, [1,2], and [4,5] are equivalent.
% changeto_1= find(T.targContrastPosIdx==2);
% T.targContrastPosIdx(changeto_1)=1;

% changeto_5= find(T.targContrastPosIdx==4);
%%
% T.targContrastPosIdx(changeto_5)=5;
leg=[];
%%


for id=1:3
    switch id
        case 1
            userows= exprows;
            col= [.2 .2 .2];
        case 2
            userows = walkingrows;
            col = [0 .7 0];
        case 3
            userows= standingrows;
            col = [.7 0 0];
    end
%     %for all exp rows, get the counts per contrast type.
     StimLevelsall = T.targContrastPosIdx(userows);
     responseall = T.targCor(userows);
%      %Each entry of StimLevelsall corresponds to single trial
     OutOfNum = ones(1,size(StimLevelsall,1));
%% 
%      %The following groups identical trials together
% %Before this line 'StimList' will have as many rows as there are trials, 
% %after this line, 'StimList' will have as many rows as there are unique 
% %stimuli. NumPos will contain the number of positive responses at each 
% %trial type, OutOfNum will contain the total number of trials at which the 
% %trial type was presented
[StimList, NumPos, OutOfNum] = PAL_MLDS_GroupTrialsbyX(StimLevelsall, responseall,...
    OutOfNum);  
%% avoid infinte values.
zerolist = find(NumPos==0);
NumPos(zerolist)= .001;
% %%
% %Parameter grid defining parameter space through which to perform a
% %brute-force search for values to be used as initial guesses in iterative
% %parameter search.
PF = @PAL_Logistic; 
searchGrid.alpha = StimList(1):.001:StimList(end);
searchGrid.beta = logspace(0,3,101);
searchGrid.gamma = 0.0;  %scalar here (since fixed) but may be vector
searchGrid.lambda = 0.02;  %ditto

%Threshold and Slope are free parameters, guess and lapse rate are fixed
paramsFree = [1 1 0 0];  %1: free parameter, 0: fixed parameter
 
[paramsValues LL exitflag] = PAL_PFML_Fit(StimList,NumPos, ...
    OutOfNum,searchGrid,paramsFree,PF);

disp('done:')
message = sprintf('Threshold estimate: %6.4f',paramsValues(1));
disp(message);
message = sprintf('Slope estimate: %6.4f\r',paramsValues(2));
disp(message);
%% plot"

%Create simple plot
ProportionCorrectObserved=NumPos./OutOfNum; 
StimLevelsFineGrain=[min(StimList):max(StimList)./1000:max(StimList)];
ProportionCorrectModel = PF(paramsValues,StimLevelsFineGrain);
 subplot(233);
title('MaxLL PF');
% axes
hold on
leg(id)=plot(StimLevelsFineGrain,ProportionCorrectModel,'-','color',col,'linewidth',4);
plot(StimList,ProportionCorrectObserved,['.'],'color', col,'markersize',40);
set(gca, 'fontsize',12);
set(gca, 'Xtick',StimList);
% axis([min(StimLevels) max(StimLevels) .4 1]);
xlabel('Stimulus Intensity');

ylabel('proportion correct');
hold on
end
legend(leg,{'exp', 'walking', 'standing'}, 'Location','SouthEast')
%%
subplot(234); cla
% mean contrast per position.
for id=1:3
    switch id
        case 1
            userows= exprows;
            col= [.2 .2 .2];
        case 2
            userows = walkingrows;
            col = [0 .7 0];
        case 3
            userows= standingrows;
            col = [.7 0 0];
    end
binnedC= zeros(1,7);
    %%
    for ibin= 1:7
    tmprows = intersect(userows, find(T.targContrastPosIdx == ibin-1));
    
    binnedC(ibin) = mean(T.targContrast(tmprows));
    end
    hold on;
    
    plot(1:7, binnedC, 'o-', 'Color', col)
end


%%
cd([datadir filesep 'Figures' filesep 'Calibration'])
    print('-dpng', [subjID ' quick summary2'])
    
end % participant
